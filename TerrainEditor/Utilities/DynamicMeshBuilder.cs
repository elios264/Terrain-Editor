using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;
using Poly2Tri;
using TerrainEditor.ViewModels;
using Polygon = Poly2Tri.Polygon;

namespace TerrainEditor.Utilities
{
    public class DynamicMeshBuilder
    {
        private readonly DynamicMesh m_mesh;

        private bool IsInverted
        {
            get { return m_mesh.FillMode == FillMode.Inverted && m_mesh.Vertices.Count > 2; }
        }
        private bool IsClosed
        {
            get { return m_mesh.IsClosed && m_mesh.Vertices.Count > 2; }
        }

        public MeshBuilder MeshEdgeData { get; }
        public MeshBuilder MeshFillData { get; }

        public DynamicMeshBuilder(DynamicMesh mesh)
        {
            m_mesh = mesh;
            MeshFillData = new MeshBuilder(false,true);
            MeshEdgeData = new MeshBuilder(false,true);
        }

        public static Model3DGroup GenerateMesh(DynamicMesh mesh)
        {
            DynamicMeshBuilder builder = new DynamicMeshBuilder(mesh);
            builder.Tesellate();

            var edgeAndFill = new List<Model3D>();

            if (builder.m_mesh.FillMode != FillMode.None)
            {
                DiffuseMaterial fillMaterial = Utils.CreateImageMaterial(mesh.UvMapping.FillTexture, true);

                fillMaterial.AmbientColor = mesh.AmbientColor;
                edgeAndFill.Add(new GeometryModel3D(builder.MeshFillData.ToMesh(true), fillMaterial) { BackMaterial = fillMaterial });
            }

            DiffuseMaterial edgeMaterial = Utils.CreateImageMaterial(mesh.UvMapping.EdgeTexture);

            edgeMaterial.AmbientColor = mesh.AmbientColor;
            edgeAndFill.Add(new GeometryModel3D(builder.MeshEdgeData.ToMesh(true), edgeMaterial) {BackMaterial = edgeMaterial});


            return new Model3DGroup { Children = new Model3DCollection(edgeAndFill) };
        }

        public void Tesellate()
        {
            MeshEdgeData.Clear();
            MeshFillData.Clear();

            var fillVertices = new List<Vector>();

            foreach (var fillPoints in GenerateSegments().Select(DrawSegment))
            {
                if (IsInverted)
                    fillPoints.Reverse();

                fillVertices.AddRange(fillPoints);
            }

            DrawFill(fillVertices);


            var newIndices =
                MeshEdgeData.TriangleIndices.Batch(3)
                    .Select(ints =>
                    {
                        var indices = ints.ToArray();
                        return new {indices, z = MeshEdgeData.Positions[indices[0]].Z };
                    })
                    .OrderBy(arg => arg.z)
                    .SelectMany(arg => arg.indices)
                    .ToList();

            MeshEdgeData.TriangleIndices.Clear();
            newIndices.ForEach(MeshEdgeData.TriangleIndices.Add);
        }

        private IEnumerable<Segment> GenerateSegments()
        {
            int size = IsClosed ? m_mesh.Vertices.Count + 1 : m_mesh.Vertices.Count;
           
            for (int i = 1; i < size; i++)
            {
                var prev2 = m_mesh.Vertices.CircularIndex(i - 2, IsClosed);
                var next = m_mesh.Vertices.CircularIndex(i + 1 , IsClosed);

                var prev = m_mesh.Vertices.CircularIndex(i - 1, IsClosed);
                var cur = m_mesh.Vertices.CircularIndex(i, IsClosed);

                Segment segment = new Segment
                {
                    PrevPrev = m_mesh.Vertices.CircularIndex(i - 3, IsClosed)?.Position,
                    Prev = m_mesh.Vertices.CircularIndex(i - 2, IsClosed)?.Position,
                    Begin = prev.Position,
                    End = cur.Position,
                    Next = m_mesh.Vertices.CircularIndex(i + 1, IsClosed)?.Position,
                    NextNext = m_mesh.Vertices.CircularIndex(i + 2, IsClosed)?.Position,
                    Direction = CalculateDirection(prev, cur),
                    PrevDirection = prev2 == null ? VertexDirection.None : CalculateDirection(prev2,prev),
                    NextDirection = next == null ? VertexDirection.None : CalculateDirection(cur,next)
                };

                if (IsInverted)
                    segment.Invert();

                yield return segment;
            }
        }
        private VertexDirection CalculateDirection(VertexInfo fst, VertexInfo snd)
        {
            if (fst.Direction != VertexDirection.Auto)
                return fst.Direction;

            var normal = (fst.Position - snd.Position).Normal();

            if (Math.Abs(normal.X) > Math.Abs(normal.Y))
            {
                return normal.X < 0
                    ? (IsInverted ? VertexDirection.Left : VertexDirection.Right)
                    : (IsInverted ? VertexDirection.Right : VertexDirection.Left);
            }

            return normal.Y < 0
                ? (IsInverted ? VertexDirection.Down : VertexDirection.Top)
                : (IsInverted ? VertexDirection.Top : VertexDirection.Down);
        }
        private List<Vector> DrawSegment(Segment segment)
        {
            var segmentUvMapping = GetUvMappingOf(segment.Direction);

            if (segmentUvMapping == null)
            {
                return new List<Vector>
                {
                    segment.Begin,
                    segment.End
                };
            }

            var bodyUvSize = m_mesh.UvMapping.ToUv(segmentUvMapping.Bodies[0]).Size;
            var unitsPerEdgeUv = CalculateUnitsPerEdgeUv();
            var bodyWidthInUnits = bodyUvSize.Width*unitsPerEdgeUv.X;
            var halfBodyHeightInUnits = bodyUvSize.Height*unitsPerEdgeUv.Y/2;

            var bodyUv = Rect.Empty;
            var start = segment.Begin;
            var smoothFactor = Math.Max(1, m_mesh.SmoothFactor);

            var doLeftCap = ShouldCloseSegment(segment, SegmentSide.Left);
            var doRightCap = ShouldCloseSegment(segment, SegmentSide.Right);

            if (doLeftCap)
                segment.PrevPrev = segment.Prev = null;

            if (doRightCap)
                segment.NextNext = segment.Next = null;

            if (segment.PrevPrev.HasValue && segment.Prev.HasValue && ShouldCloseSegment(new Segment { Prev = segment.PrevPrev, Begin = segment.Prev.Value, End = segment.Begin, }, SegmentSide.Left))
                segment.PrevPrev = null;

            var prevNumOfCuts = (double)Math.Max((int)Math.Floor((segment.Begin - (segment.Prev ?? segment.Begin)).Length / (bodyWidthInUnits + m_mesh.StrechThreshold)), 1) * smoothFactor;
            var endPrevious = Utils.HermiteLerp(segment.PrevPrev ?? segment.Prev ?? segment.Begin, segment.Prev ??  segment.Begin, segment.Begin, segment.End, prevNumOfCuts == 1 ? 0.001 : ((prevNumOfCuts - 1) / prevNumOfCuts));
            var startOffset = (start - endPrevious).Normal() * halfBodyHeightInUnits;

            if (doLeftCap)
                DrawCap(segmentUvMapping.LeftCap, SegmentSide.Left, segment.Begin + startOffset, segment.Begin - startOffset, segmentUvMapping.ZOffset);

            if (doLeftCap && doRightCap)
                smoothFactor = 1;

            var numberOfCuts = Math.Max((int) Math.Floor((segment.End - segment.Begin).Length/(bodyWidthInUnits + m_mesh.StrechThreshold)),1)*smoothFactor;
            var fillPoints = new List<Vector>(numberOfCuts);

            for (int i = 0; i < numberOfCuts; i++)
            {
                var percentEnd = (i + 1) / (double)numberOfCuts;

                var end = Utils.HermiteLerp(segment.Prev ?? segment.Begin, segment.Begin, segment.End, segment.Next ?? segment.End, percentEnd);
                var endOffset = (end - start).Normal()*halfBodyHeightInUnits;

                var localTopLeft = start + startOffset;
                var localTopRight = end + endOffset;
                var localBottomLeft = start - startOffset;
                var localBottomRight = end - endOffset;

                fillPoints.Add(start);

                start = end;
                startOffset = endOffset;

                if (i%smoothFactor == 0)
                {
                    bodyUv = m_mesh.UvMapping.ToUv( segmentUvMapping.Bodies[Math.Abs(percentEnd.GetHashCode()%segmentUvMapping.Bodies.Count)]);
                    bodyUv.Width /= smoothFactor;
                }
                else
                    bodyUv.X += bodyUv.Width;

                MeshEdgeData.AddQuad(
                    new Point3D(localBottomLeft.X, localBottomLeft.Y, segmentUvMapping.ZOffset),
                    new Point3D(localTopLeft.X, localTopLeft.Y, segmentUvMapping.ZOffset),
                    new Point3D(localTopRight.X, localTopRight.Y, segmentUvMapping.ZOffset),
                    new Point3D(localBottomRight.X, localBottomRight.Y, segmentUvMapping.ZOffset),
                    bodyUv.BottomLeft,bodyUv.TopLeft,bodyUv.TopRight,bodyUv.BottomRight);
            }

            if(doRightCap)
                DrawCap(segmentUvMapping.RightCap, SegmentSide.Right, segment.End + startOffset, segment.End - startOffset, segmentUvMapping.ZOffset);

            return fillPoints;
        }

        private void DrawCap(Rect rect, SegmentSide side, Vector top, Vector bottom, double zOffset)
        {
            var capUv = m_mesh.UvMapping.ToUv(rect);
            var capOffset = (bottom - top).Normal()*capUv.Size.Width*CalculateUnitsPerEdgeUv().X;

            var otherTop = side == SegmentSide.Left ? top - capOffset : top + capOffset;
            var otherBottom = side == SegmentSide.Left ? bottom - capOffset : bottom + capOffset;

            if (side == SegmentSide.Left)
            {
                Utils.Swap(ref top,ref otherTop);
                Utils.Swap(ref bottom,ref otherBottom);
            }

            MeshEdgeData.AddQuad(
                new Point3D(bottom.X, bottom.Y, zOffset), 
                new Point3D(top.X,top.Y,zOffset), 
                new Point3D(otherTop.X,otherTop.Y,zOffset), 
                new Point3D(otherBottom.X,otherBottom.Y,zOffset),
                capUv.BottomLeft,capUv.TopLeft,capUv.TopRight,capUv.BottomRight);
        }
        private void DrawFill(List<Vector> fillVertices)
        {
            if (m_mesh.Vertices.Count <= 2 || m_mesh.FillMode == FillMode.None)
                return;

            if (!m_mesh.IsClosed)
                fillVertices.Add(m_mesh.FillMode != FillMode.Inverted ? m_mesh.Vertices.Last().Position : m_mesh.Vertices.First().Position);


            var polygon = new Polygon(fillVertices.Select(v => new PolygonPoint(v.X,v.Y)));

            if (IsInverted)
            {
                var center = polygon.GetCentroid();
                var size = new Size(polygon.BoundingBox.Width,polygon.BoundingBox.Height);

                var topLeft = new Point(center.X - size.Width, center.Y + size.Height);
                var topRight = new Point(center.X + size.Width, center.Y + size.Height);
                var bottomLeft = new Point(center.X - size.Width, center.Y - size.Height);
                var bottomRight = new Point(center.X + size.Width, center.Y - size.Height);

                var invertedPolygon = new Polygon(
                    new PolygonPoint(bottomLeft.X, bottomLeft.Y),
                    new PolygonPoint(topLeft.X, topLeft.Y),
                    new PolygonPoint(topRight.X, topRight.Y),
                    new PolygonPoint(bottomRight.X, bottomRight.Y));

                invertedPolygon.AddHole(polygon);

                polygon = invertedPolygon;
            }

            P2T.Triangulate(polygon);

            var unitsPerFill = CalculateUnitsPerFillUv();
            foreach (var triangle in polygon.Triangles)
            {
                MeshFillData.AddTriangle(
                    new Point3D(triangle.Points._0.X, triangle.Points._0.Y,0.0),
                    new Point3D(triangle.Points._1.X, triangle.Points._1.Y,0.0),
                    new Point3D(triangle.Points._2.X, triangle.Points._2.Y,0.0),
                    new Point(triangle.Points._0.X / unitsPerFill.X, triangle.Points._0.Y / unitsPerFill.Y),
                    new Point(triangle.Points._1.X / unitsPerFill.X, triangle.Points._1.Y / unitsPerFill.Y),
                    new Point(triangle.Points._2.X / unitsPerFill.X, triangle.Points._2.Y / unitsPerFill.Y));
            }
        }

        private bool ShouldCloseSegment(Segment segment, SegmentSide side)
        {
            if (IsInverted)
                side = side == SegmentSide.Left ? SegmentSide.Right : SegmentSide.Left;

            if (m_mesh.SplitWhenDifferent && (side == SegmentSide.Left && segment.Direction != segment.PrevDirection || (side == SegmentSide.Right && segment.Direction != segment.NextDirection)))
                return true;

            var angle = side == SegmentSide.Left ? segment.AngleWithPrev : segment.AngleWithNext;

            if (angle < m_mesh.SplitCornersThreshold || angle > (360 - m_mesh.SplitCornersThreshold))
                return true;

            return angle == 180 && !(side == SegmentSide.Left ? segment.Prev.HasValue : segment.Next.HasValue);
        }
        private UvMapping.Segment GetUvMappingOf(VertexDirection direction)
        {
            switch (direction)
            {
                case VertexDirection.Top:
                    return m_mesh.UvMapping.Top;
                case VertexDirection.Down:
                    return m_mesh.UvMapping.Bottom ?? m_mesh.UvMapping.Top;
                case VertexDirection.Left:
                    return m_mesh.UvMapping.Left ?? m_mesh.UvMapping.Right ?? m_mesh.UvMapping.Top;
                case VertexDirection.Right:
                    return m_mesh.UvMapping.Right ?? m_mesh.UvMapping.Left ?? m_mesh.UvMapping.Top;
                default:
                    return null;
            }
        }
        private Vector CalculateUnitsPerEdgeUv()
        {
            return new Vector(m_mesh.UvMapping.EdgeTexture.PixelWidth / (double)m_mesh.PixelsPerUnit, m_mesh.UvMapping.EdgeTexture.PixelHeight / (double)m_mesh.PixelsPerUnit);
        }
        private Vector CalculateUnitsPerFillUv()
        {
            return new Vector(m_mesh.UvMapping.FillTexture.PixelWidth / (double)m_mesh.PixelsPerUnit, m_mesh.UvMapping.FillTexture.PixelHeight / (double)m_mesh.PixelsPerUnit);
        }

        private enum SegmentSide
        {
            Left, Right
        }
        private class Segment
        {
            public VertexDirection Direction, PrevDirection, NextDirection;

            public Vector Begin, End;
            public Vector? PrevPrev, Prev, Next, NextNext;

            public double AngleWithPrev
            {
                get
                {
                    if (!Prev.HasValue)
                        return 180;

                    double angle = Vector.AngleBetween(End - Begin, Prev.Value - Begin);
                    return angle < 0 ? angle + 360 : angle;
                }
            }

            public double AngleWithNext
            {
                get
                {
                    if (!Next.HasValue)
                        return 180;

                    double angle = Vector.AngleBetween(Begin - End, Next.Value - End);
                    return angle < 0 ? angle + 360 : angle;
                }
            }

            public void Invert()
            {
                Utils.Swap(ref Begin, ref End);
                Utils.Swap(ref Prev, ref Next);
                Utils.Swap(ref PrevPrev, ref NextNext);
                Utils.Swap(ref PrevDirection, ref NextDirection);
            }
        }
    }
}