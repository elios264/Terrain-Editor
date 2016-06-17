using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;
using Poly2Tri;
using TerrainEditor.Utilities;
using TerrainEditor.ViewModels;
using Polygon = Poly2Tri.Polygon;

namespace TerrainEditor.Core
{
    public class TerrainMeshBuilder
    {
        private bool IsClosed
        {
            get { return Mesh.IsClosed && Mesh.Vertices.Count > 2; }
        }
        private bool IsInverted
        {
            get { return Mesh.FillMode == FillMode.Inverted && Mesh.Vertices.Count > 2; }
        }

        public Terrain Mesh { get; set; }
        public MeshBuilder MeshEdgeData { get; }
        public MeshBuilder MeshFillData { get; }

        public TerrainMeshBuilder()
        {
            MeshFillData = new MeshBuilder(false,true);
            MeshEdgeData = new MeshBuilder(false,true);
        }
        public void Tesellate()
        {
            MeshEdgeData.Clear();
            MeshFillData.Clear();

            if (Mesh.UvMapping.EdgeTexture == null)
                return;

            var fillVertices = new List<Vector>();
            foreach (var fillPoints in GenerateSegments().Select(TesellateSegment))
            {
                if (IsInverted)
                    fillPoints.Reverse();

                fillVertices.AddRange(fillPoints);
            }

            TesellateFill(fillVertices);

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
            int size = IsClosed ? Mesh.Vertices.Count + 1 : Mesh.Vertices.Count;
           
            for (int i = 1; i < size; i++)
            {
                var prev2 = Mesh.Vertices.ElementAt(i - 2, IsClosed);
                var next = Mesh.Vertices.ElementAt(i + 1 , IsClosed);

                var prev = Mesh.Vertices.ElementAt(i - 1, IsClosed);
                var cur = Mesh.Vertices.ElementAt(i, IsClosed);

                Segment segment = new Segment
                {
                    PrevPrev = Mesh.Vertices.ElementAt(i - 3, IsClosed)?.Position,
                    Prev = Mesh.Vertices.ElementAt(i - 2, IsClosed)?.Position,
                    Begin = prev.Position,
                    End = cur.Position,
                    Next = Mesh.Vertices.ElementAt(i + 1, IsClosed)?.Position,
                    NextNext = Mesh.Vertices.ElementAt(i + 2, IsClosed)?.Position,
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
        private List<Vector> TesellateSegment(Segment segment)
        {
            var curSegmentMapping = GetUvMappingOf(segment.Direction);

            if (curSegmentMapping.Bodies.Count == 0)
                return new List<Vector> { segment.Begin, segment.End };

            var bodySizeInUV = Mesh.UvMapping.ToUV(curSegmentMapping.Bodies[0]).Size;
            var edgeUVSizeInUnits = GetEdgeUVSizeInUnits();
            var bodySizeInUnits = new Size(bodySizeInUV.Width * edgeUVSizeInUnits.X, bodySizeInUV.Height * edgeUVSizeInUnits.Y);
        

            var doLeftCap = ShouldCloseSegment(segment, SegmentSide.Left);
            var doRightCap = ShouldCloseSegment(segment, SegmentSide.Right);

            if (doLeftCap)
                segment.PrevPrev = segment.Prev = null;

            if (doRightCap)
                segment.NextNext = segment.Next = null;

            if (segment.PrevPrev.HasValue && segment.Prev.HasValue && ShouldCloseSegment(new Segment { Prev = segment.PrevPrev, Begin = segment.Prev.Value, End = segment.Begin, }, SegmentSide.Left))
                segment.PrevPrev = null;

            var currentCutUV = Rect.Empty;
            var start = segment.Begin;
            var smoothFactor = Math.Max(1, Mesh.SmoothFactor);

            var prevNumOfCuts = (double)Math.Max((int)Math.Floor((segment.Begin - (segment.Prev ?? segment.Begin)).Length / (bodySizeInUnits.Width + Mesh.StrechThreshold)), 1) * smoothFactor;
            var endPrevious = Utils.HermiteLerp(segment.PrevPrev ?? segment.Prev ?? segment.Begin, segment.Prev ??  segment.Begin, segment.Begin, segment.End, prevNumOfCuts == 1 ? 0.001 : ((prevNumOfCuts - 1) / prevNumOfCuts));
            var startOffset = (start - endPrevious).Normal() * (bodySizeInUnits.Height / 2);

          //  if (doLeftCap)
                TesellateCap(curSegmentMapping.LeftCap, SegmentSide.Left, segment.Begin + startOffset, segment.Begin - startOffset, curSegmentMapping.ZOffset);

            if (doLeftCap && doRightCap)
                smoothFactor = 1;

            var numberOfCuts = Math.Max((int) Math.Floor((segment.End - segment.Begin).Length/(bodySizeInUnits.Width + Mesh.StrechThreshold)),1)*smoothFactor;
            var fillPoints = new List<Vector>(numberOfCuts);

            for (int i = 0; i < numberOfCuts; i++)
            {
                var percentEnd = (i + 1) / (double)numberOfCuts;

                var end = Utils.HermiteLerp(segment.Prev ?? segment.Begin, segment.Begin, segment.End, segment.Next ?? segment.End, percentEnd);
                var endOffset = (end - start).Normal()* (bodySizeInUnits.Height/2);

                var localTopLeft = start + startOffset;
                var localTopRight = end + endOffset;
                var localBottomLeft = start - startOffset;
                var localBottomRight = end - endOffset;

                fillPoints.Add(start);

                start = end;
                startOffset = endOffset;

                if (i%smoothFactor == 0)
                {
                    currentCutUV = Mesh.UvMapping.ToUV( curSegmentMapping.Bodies[Math.Abs(percentEnd.GetHashCode()%curSegmentMapping.Bodies.Count)]);
                    currentCutUV.Width /= smoothFactor;
                }
                else
                    currentCutUV.X += currentCutUV.Width;

                MeshEdgeData.AddQuad(
                    new Point3D(localBottomLeft.X, localBottomLeft.Y, curSegmentMapping.ZOffset),
                    new Point3D(localTopLeft.X, localTopLeft.Y, curSegmentMapping.ZOffset),
                    new Point3D(localTopRight.X, localTopRight.Y, curSegmentMapping.ZOffset),
                    new Point3D(localBottomRight.X, localBottomRight.Y, curSegmentMapping.ZOffset),
                    currentCutUV.BottomLeft,currentCutUV.TopLeft,currentCutUV.TopRight,currentCutUV.BottomRight);
            }

          //  if(doRightCap)
                TesellateCap(curSegmentMapping.RightCap, SegmentSide.Right, segment.End + startOffset, segment.End - startOffset, curSegmentMapping.ZOffset);

            return fillPoints;
        }

        private void TesellateCap(Rect rect, SegmentSide side, Vector top, Vector bottom, double zOffset)
        {
            var capUv = Mesh.UvMapping.ToUV(rect);
            var capOffset = (bottom - top).Normal()*capUv.Size.Width*GetEdgeUVSizeInUnits().X;

            var otherTop = side == SegmentSide.Left ? top - capOffset : top + capOffset;
            var otherBottom = side == SegmentSide.Left ? bottom - capOffset : bottom + capOffset;

            if (side == SegmentSide.Left)
            {
                Utils.Swap(ref top, ref otherTop);
                Utils.Swap(ref bottom, ref otherBottom);
            }

            MeshEdgeData.AddQuad(
                new Point3D(bottom.X, bottom.Y, zOffset), 
                new Point3D(top.X,top.Y,zOffset), 
                new Point3D(otherTop.X,otherTop.Y,zOffset), 
                new Point3D(otherBottom.X,otherBottom.Y,zOffset),
                capUv.BottomLeft,capUv.TopLeft,capUv.TopRight,capUv.BottomRight);
        }
        private void TesellateFill(List<Vector> fillVertices)
        {
            if (Mesh.Vertices.Count <= 2 || Mesh.FillMode == FillMode.None || Mesh.UvMapping.FillTexture == null)
                return;

            if (!Mesh.IsClosed)
                fillVertices.Add(Mesh.FillMode != FillMode.Inverted ? Mesh.Vertices.Last().Position : Mesh.Vertices.First().Position);


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

            try
            {
                P2T.Triangulate(polygon);
            }
            catch (Exception)
            {
                return;
            }

            var unitsPerFill = GetFillUVSizeInUnits();
            foreach (var triangle in polygon.Triangles)
            {
                MeshFillData.AddTriangle(
                    new Point3D(triangle.Points._0.X, triangle.Points._0.Y,0.0),
                    new Point3D(triangle.Points._1.X, triangle.Points._1.Y,0.0),
                    new Point3D(triangle.Points._2.X, triangle.Points._2.Y,0.0),
                    new Point(triangle.Points._0.X / unitsPerFill.X, 1 - triangle.Points._0.Y / unitsPerFill.Y),
                    new Point(triangle.Points._1.X / unitsPerFill.X, 1 - triangle.Points._1.Y / unitsPerFill.Y),
                    new Point(triangle.Points._2.X / unitsPerFill.X, 1 - triangle.Points._2.Y / unitsPerFill.Y));
            }
        }

        private bool ShouldCloseSegment(Segment segment, SegmentSide side)
        {
            if (IsInverted)
                side = side == SegmentSide.Left ? SegmentSide.Right : SegmentSide.Left;

            if (Mesh.SplitWhenDifferent && (side == SegmentSide.Left && segment.Direction != segment.PrevDirection || (side == SegmentSide.Right && segment.Direction != segment.NextDirection)))
                return true;

            var angle = side == SegmentSide.Left ? segment.AngleWithPrev : segment.AngleWithNext;

            if (angle < Mesh.SplitCornersThreshold || angle > (360 - Mesh.SplitCornersThreshold))
                return true;

            return angle == 180 && !(side == SegmentSide.Left ? segment.Prev.HasValue : segment.Next.HasValue);
        }
        private ViewModels.Segment GetUvMappingOf(VertexDirection direction)
        {
            switch (direction)
            {
                case VertexDirection.Top:
                    return Mesh.UvMapping.Top;
                case VertexDirection.Down:
                    return Mesh.UvMapping.Bottom ?? Mesh.UvMapping.Top;
                case VertexDirection.Left:
                    return Mesh.UvMapping.Left ?? Mesh.UvMapping.Right ?? Mesh.UvMapping.Top;
                case VertexDirection.Right:
                    return Mesh.UvMapping.Right ?? Mesh.UvMapping.Left ?? Mesh.UvMapping.Top;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private Vector GetEdgeUVSizeInUnits()
        {
            return new Vector(
                Mesh.UvMapping.EdgeTexture.PixelWidth / (double)Mesh.PixelsPerUnit, 
                Mesh.UvMapping.EdgeTexture.PixelHeight / (double)Mesh.PixelsPerUnit);
        }
        private Vector GetFillUVSizeInUnits()
        {
            return new Vector(
                Mesh.UvMapping.FillTexture.PixelWidth / (double)Mesh.PixelsPerUnit, 
                Mesh.UvMapping.FillTexture.PixelHeight / (double)Mesh.PixelsPerUnit);
        }

        public static Model3DGroup GenerateMesh(Terrain mesh)
        {
            if (mesh.UvMapping == null)
            {
                return new Model3DGroup();
            }

            var builder = new TerrainMeshBuilder { Mesh = mesh };
            builder.Tesellate();

            var edgeAndFill = new List<Model3D>();

            if (builder.Mesh.FillMode != FillMode.None)
            {
                DiffuseMaterial fillMaterial = Utils.CreateImageMaterial(mesh.UvMapping.FillTexture, true);

                fillMaterial.AmbientColor = mesh.AmbientColor;
                edgeAndFill.Add(new GeometryModel3D(builder.MeshFillData.ToMesh(true), fillMaterial) { BackMaterial = fillMaterial });
            }

            if (mesh.UvMapping.EdgeTexture != null)
            {
                DiffuseMaterial edgeMaterial = Utils.CreateImageMaterial(mesh.UvMapping.EdgeTexture);

                edgeMaterial.AmbientColor = mesh.AmbientColor;
                edgeAndFill.Add(new GeometryModel3D(builder.MeshEdgeData.ToMesh(true), edgeMaterial) { BackMaterial = edgeMaterial });
            }

            return new Model3DGroup { Children = new Model3DCollection(edgeAndFill) };
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