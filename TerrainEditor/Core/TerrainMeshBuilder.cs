using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;
using Poly2Tri;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;
using Polygon = Poly2Tri.Polygon;

namespace TerrainEditor.Core
{
    public class TerrainMeshBuilder
    {
        private bool IsClosed
        {
            get { return Terrain.IsClosed && Terrain.Vertices.Count > 2; }
        }
        private bool IsInverted
        {
            get { return Terrain.FillMode == FillMode.Inverted && Terrain.Vertices.Count > 2; }
        }

        private IEnumerable<Edge> Edges
        {
            get
            {
                int size = IsClosed ? Terrain.Vertices.Count + 1 : Terrain.Vertices.Count;

                for (int i = 1; i < size; i++)
                {
                    var prev2 = Terrain.Vertices.At(i - 2, IsClosed);
                    var next = Terrain.Vertices.At(i + 1, IsClosed);

                    var prev = Terrain.Vertices.At(i - 1, IsClosed);
                    var cur = Terrain.Vertices.At(i, IsClosed);

                    var edge = new Edge
                    {
                        Prev = Terrain.Vertices.At(i - 2, IsClosed)?.Position,
                        Begin = prev.Position,
                        End = cur.Position,
                        Next = Terrain.Vertices.At(i + 1, IsClosed)?.Position,
                        Direction = DirectionFor(prev, cur),
                        PrevDirection = prev2 == null ? VertexDirection.None : DirectionFor(prev2, prev),
                        NextDirection = next == null ? VertexDirection.None : DirectionFor(cur, next),
                        Split = prev.Split,
                        PrevSplit = prev2?.Split ?? SplitMode.No,
                        NextSplit = cur?.Split ?? SplitMode.No,
                    };
                    if (IsInverted) edge.Invert();

                    yield return edge;
                }
            }
        }

        public Terrain Terrain { get; set; }
        public MeshBuilder EdgeData { get; }
        public MeshBuilder FillData { get; }

        public TerrainMeshBuilder()
        {
            FillData = new MeshBuilder(false,true);
            EdgeData = new MeshBuilder(false,true);
        }

        public void Tesellate()
        {
            List<Vector> edgeCentalPoints = null;

            EdgeData.Clear();
            FillData.Clear();

            if (Terrain.UvMapping.EdgeTexture != null && Terrain.UvMapping.Top.Bodies.Count > 0)
            {
                TesellateEdges(out edgeCentalPoints);

                //Wpf needs the triangles to be sorted by depth
                var triangleIndices = (List<int>)EdgeData.TriangleIndices;
                var sortedIndices = EdgeData.TriangleIndices
                    .Batch(3)
                    .Select(ints => { var indices = ints.ToArray(); return new { indices, z = EdgeData.Positions[indices[0]].Z }; })
                    .OrderBy(arg => arg.z)
                    .SelectMany(arg => arg.indices)
                    .ToList();
                triangleIndices.Clear();
                triangleIndices.AddRange(sortedIndices);
            }

            if (Terrain.UvMapping.FillTexture != null)
            {
                TesellateFill(edgeCentalPoints);
            }
        }
        private void TesellateEdges(out List<Vector> centralPoints)
        {
            centralPoints = new List<Vector>();

            var normalStart = (Vector?)null;
            var firstCapDone = (bool?)null;
            var pixelsPerUnit = Terrain.PixelsPerUnit;

            var batches = (IsInverted ? Edges.Reverse() : Edges).BatchOfTakeUntil(e => ShouldCloseEdge(e, false)).ToArray();
            var begin = batches[0][0].Begin;

            foreach (var batch in batches)
            {
                if (Terrain.SplitWhenDifferent
                    || batch[0].Split == SplitMode.Left || batch[0].Split == SplitMode.Both
                    || batch[0].PrevSplit == SplitMode.Both || batch[0].PrevSplit == SplitMode.Right)
                {
                    normalStart = null;
                    batch[0].Prev = null;
                }
                var lst = batch.Length - 1;
                if (Terrain.SplitWhenDifferent
                    || batch[lst].Split == SplitMode.Right || batch[lst].Split == SplitMode.Both
                    || batch[lst].NextSplit == SplitMode.Both || batch[lst].NextSplit == SplitMode.Left)
                {
                    batch[lst].Next = null;
                }

                double[] edgesLengths;
                var segmentMapping = SegmentFor(batch[0].Direction);
                var smoothFactor = batch.Length == 1 && batch[0].Prev == null && batch[lst].Next == null ? 1 : Terrain.SmoothFactor;
                var totalLength = InterpolateLength(Terrain.InterpolationMode, batch, smoothFactor * 5, out edgesLengths);
                var bodyCount = Math.Max((int)Math.Round(totalLength / (segmentMapping.BodySize.Width / pixelsPerUnit) + Terrain.StrechThreshold), 1);
                var finalBodySize = new Size(totalLength / bodyCount, segmentMapping.BodySize.Height / pixelsPerUnit);
                var halfFinalBodySizeHeight = finalBodySize.Height / 2;
                var incLength = finalBodySize.Width / smoothFactor;
                var currentLength = incLength;
                var first = true;
                var offsets = segmentMapping.Offsets;
                offsets = new Vector3D(offsets.X / pixelsPerUnit,offsets.Y / pixelsPerUnit, offsets.Z / pixelsPerUnit);


                for (int i = 0; i < bodyCount; i++)
                {
                    var bodyUV = Terrain.UvMapping.ToUV(new Rect(segmentMapping.Bodies[Math.Abs(begin.GetHashCode() % segmentMapping.Bodies.Count)], segmentMapping.BodySize));
                    bodyUV.Width /= smoothFactor;

                    for (int j = 0; j < smoothFactor; j++, currentLength += incLength)
                    {
                        var end = Interpolate(Terrain.InterpolationMode, batch, edgesLengths, currentLength);
                        var normalEnd = (end - begin).Normal();
                        var endOffset = normalEnd * halfFinalBodySizeHeight;
                        var beginOffset = (normalStart ?? normalEnd) * halfFinalBodySizeHeight;

                        var yOffset = (normalStart ?? normalEnd) * offsets.Y;
                        var localBottomLeft = begin - beginOffset + yOffset;
                        var localTopLeft = begin + beginOffset + yOffset;

                        yOffset = normalEnd * offsets.Y;
                        var localBottomRight = end - endOffset + yOffset;
                        var localTopRight = end + endOffset + yOffset;

                        EdgeData.AddQuad(
                            new Point3D(localBottomLeft.X, localBottomLeft.Y, offsets.Z),
                            new Point3D(localTopLeft.X, localTopLeft.Y, offsets.Z),
                            new Point3D(localTopRight.X, localTopRight.Y, offsets.Z),
                            new Point3D(localBottomRight.X, localBottomRight.Y, offsets.Z),
                            bodyUV.BottomLeft, bodyUV.TopLeft, bodyUV.TopRight, bodyUV.BottomRight);

                        if (first)
                        {
                            first = TesellateCap(new Rect(segmentMapping.LeftCap, segmentMapping.CapSize), true, batch[0], normalStart ?? normalEnd, offsets);
                            firstCapDone = firstCapDone ?? first;
                            first = false;
                        }

                        centralPoints.Add(begin);
                        normalStart = normalEnd;
                        bodyUV.X += bodyUV.Width;
                        begin = end;
                    }
                }
                TesellateCap(new Rect(segmentMapping.RightCap, segmentMapping.CapSize), false, batch[lst], normalStart.Value, offsets);
            }
            //close the terrain
            //if (IsClosed && batches[0][0].Prev != null)
            //{
            //    var offsets = SegmentFor(batches[0][0].Direction).Offsets;

            //    var offsetY = offsets.Y / pixelsPerUnit * normalStart.Value;
            //    var offset = normalStart.Value * ((EdgeData.Positions[1] - EdgeData.Positions[0]).Length / 2);
            //    var bl = batches[0][0].Begin - offset + offsetY;
            //    var tl = batches[0][0].Begin + offset + offsetY;

            //    EdgeData.Positions[0] = new Point3D(bl.X, bl.Y, EdgeData.Positions[0].Z);
            //    EdgeData.Positions[1] = new Point3D(tl.X, tl.Y, EdgeData.Positions[1].Z);

            //    if (firstCapDone ?? false)
            //    {
            //        var horizontalNormal = (EdgeData.Positions[1].ToVector(10) - EdgeData.Positions[0].ToVector(10)).Normal() * (offsets.X / pixelsPerUnit);

            //        EdgeData.Positions[6] = EdgeData.Positions[1] + new Vector3D(horizontalNormal.X, horizontalNormal.Y,0);
            //        EdgeData.Positions[7] = EdgeData.Positions[0] + new Vector3D(horizontalNormal.X, horizontalNormal.Y, 0);
            //    }
            //}
        }
        private bool TesellateCap(Rect area, bool left, Edge edge, Vector verticalNormal, Vector3D offset)
        {
            if (!ShouldCloseEdge(edge, left) || area == default(Rect))
                return false;

            var capUv = Terrain.UvMapping.ToUV(area);
            var capSize = new Size(area.Width / Terrain.PixelsPerUnit, area.Height / Terrain.PixelsPerUnit);

            var top = (left ? edge.Begin : edge.End) + verticalNormal * (capSize.Height / 2) + verticalNormal * offset.Y;
            var bottom = (left ? edge.Begin : edge.End) - verticalNormal * (capSize.Height / 2) + verticalNormal * offset.Y;

            var horizontalNormal = (bottom - top).Normal();

            top += horizontalNormal * offset.X * (left ? -1 : 1);
            bottom += horizontalNormal * offset.X * (left ? -1 : 1);

            var capOffset = horizontalNormal * capSize.Width;
            var otherTop = left ? top - capOffset : top + capOffset ;
            var otherBottom = left ? bottom - capOffset : bottom + capOffset ;

            if (left)
            {
                Utils.Swap(ref top, ref otherTop);
                Utils.Swap(ref bottom, ref otherBottom);
            }

            EdgeData.AddQuad(
                new Point3D(bottom.X, bottom.Y, offset.Z),
                new Point3D(top.X, top.Y, offset.Z),
                new Point3D(otherTop.X, otherTop.Y, offset.Z),
                new Point3D(otherBottom.X, otherBottom.Y, offset.Z),
                capUv.BottomLeft, capUv.TopLeft, capUv.TopRight, capUv.BottomRight);

            return true;
        }
        private void TesellateFill(List<Vector> edgeVertices)
        {
            if (Terrain.Vertices.Count <= 2 || Terrain.FillMode == FillMode.None)
                return;

            edgeVertices = edgeVertices == null || edgeVertices.Count > 10000
                ? Terrain.Vertices.Select(v => v.Position).ToList()
                : edgeVertices;

            if (!Terrain.IsClosed)
                edgeVertices.Add(Terrain.FillMode != FillMode.Inverted ? Terrain.Vertices.Last().Position : Terrain.Vertices.First().Position);

            var polygon = new Polygon(edgeVertices.Select(v => new PolygonPoint(v.X,v.Y)));
            polygon.Simplify();

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

            var unitsPerFill = new Size(
                Terrain.UvMapping.FillTexture.PixelWidth / (double)Terrain.PixelsPerUnit,
                Terrain.UvMapping.FillTexture.PixelHeight / (double)Terrain.PixelsPerUnit);
            foreach (var triangle in polygon.Triangles)
            {
                FillData.AddTriangle(
                    new Point3D(triangle.Points._0.X, triangle.Points._0.Y,0.0),
                    new Point3D(triangle.Points._1.X, triangle.Points._1.Y,0.0),
                    new Point3D(triangle.Points._2.X, triangle.Points._2.Y,0.0),
                    new Point(triangle.Points._0.X / unitsPerFill.Width, 1 - triangle.Points._0.Y / unitsPerFill.Height),
                    new Point(triangle.Points._1.X / unitsPerFill.Width, 1 - triangle.Points._1.Y / unitsPerFill.Height),
                    new Point(triangle.Points._2.X / unitsPerFill.Width, 1 - triangle.Points._2.Y / unitsPerFill.Height));
            }
        }

        private Segment SegmentFor(VertexDirection direction)
        {
            switch (direction)
            {
            case VertexDirection.None:
            case VertexDirection.Top:  return Terrain.UvMapping.Top;
            case VertexDirection.Down: return Terrain.UvMapping.Bottom;
            case VertexDirection.Left: return Terrain.UvMapping.Left;
            case VertexDirection.Right:return Terrain.UvMapping.Right;
            default: throw new ArgumentOutOfRangeException();
            }
        }
        private VertexDirection DirectionFor(VertexInfo fst, VertexInfo snd)
        {
            var normal = (fst.Position - snd.Position).Normal();
            var candidate = fst.Direction != VertexDirection.Auto
                ? fst.Direction
                : (Math.Abs(normal.X) > Math.Abs(normal.Y)
                    ? (normal.X < 0
                        ? (IsInverted
                            ? VertexDirection.Left
                            : VertexDirection.Right)
                        : (IsInverted
                            ? VertexDirection.Right
                            : VertexDirection.Left))
                    : (normal.Y < 0
                        ? (IsInverted
                            ? VertexDirection.Down
                            : VertexDirection.Top)
                        : (IsInverted
                            ? VertexDirection.Top
                            : VertexDirection.Down)));

            switch (candidate)
            {
            case VertexDirection.Top:
                return candidate;
            case VertexDirection.Down:
                return Terrain.UvMapping?.Bottom?.Bodies.Count > 0 ? candidate : VertexDirection.Top;
            case VertexDirection.Left:
                return Terrain.UvMapping?.Left?.Bodies.Count > 0 ? candidate : (Terrain.UvMapping?.Right?.Bodies.Count > 0 ? VertexDirection.Right : VertexDirection.Top);
            case VertexDirection.Right:
                return Terrain.UvMapping?.Right?.Bodies.Count > 0 ? candidate : (Terrain.UvMapping?.Left?.Bodies.Count > 0 ? VertexDirection.Left : VertexDirection.Top);
            }

            return VertexDirection.None;
        }

        private static bool ShouldCloseEdge(Edge edge, bool left)
        {
            return left
                ? edge.Split == SplitMode.Both
                  || edge.Split == SplitMode.Left
                  || edge.PrevSplit == SplitMode.Both
                  || edge.PrevSplit == SplitMode.Right
                  || edge.PrevDirection == VertexDirection.None
                  || edge.PrevDirection != edge.Direction
                : edge.Split == SplitMode.Both
                  || edge.Split == SplitMode.Right
                  || edge.NextSplit == SplitMode.Both
                  || edge.NextSplit == SplitMode.Left
                  || edge.NextDirection == VertexDirection.None
                  || edge.NextDirection != edge.Direction;
        }
        private static Vector Interpolate(InterpolationMode mode ,Edge[] edges, double[] edgesLengths, double length)
        {
            var i = 0;
            var percentaje = 1.0;
            for (; i < edgesLengths.Length; i++)
            {
                if (length <= edgesLengths[i])
                {
                    percentaje = length / edgesLengths[i];
                    break;
                }
                length -= edgesLengths[i];
            }
            if (i == edgesLengths.Length)
                i--;

            return Interpolate(mode ,edges[i].Prev ?? edges[i].Begin, edges[i].Begin, edges[i].End, edges[i].Next ?? edges[i].End, percentaje);
        }
        private static Vector Interpolate(InterpolationMode mode ,Vector a, Vector b, Vector c, Vector d, double percentaje)
        {
            switch (mode)
            {
            case InterpolationMode.Hermite: return Utils.HermiteInterpolate(a, b, c, d, percentaje);
            case InterpolationMode.Cubic: return Utils.CubicInterpolate(a, b, c, d, percentaje);
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
        private static double InterpolateLength(InterpolationMode mode,IEnumerable<Edge> edges, int resolution, out double[] partialLengths)
        {
            partialLengths = edges
                .Select(e => Enumerable
                    .Range(0, resolution + 1)
                    .Select(cur => Interpolate(mode ,e.Prev ?? e.Begin, e.Begin, e.End, e.Next ?? e.End, 1.0 / resolution * cur))
                    .Pairwise((v1, v2) => (v2 - v1).Length)
                    .Sum())
                .ToArray();

            return partialLengths.Sum();
        }

        public static Model3DGroup GenerateMesh(Terrain mesh)
        {
            if (mesh.UvMapping == null)
            {
                return new Model3DGroup();
            }

            var builder = new TerrainMeshBuilder { Terrain = mesh };
            builder.Tesellate();

            var edgeAndFill = new List<Model3D>();

            if (builder.Terrain.FillMode != FillMode.None)
            {
                var fillMaterial = Utils.CreateImageMaterial(mesh.UvMapping.FillTexture, true);

                fillMaterial.AmbientColor = mesh.AmbientColor;
                edgeAndFill.Add(new GeometryModel3D(builder.FillData.ToMesh(true), fillMaterial)
                {
                    BackMaterial = fillMaterial
                });
            }

            if (mesh.UvMapping.EdgeTexture != null)
            {
                var edgeMaterial = Utils.CreateImageMaterial(mesh.UvMapping.EdgeTexture);

                edgeMaterial.AmbientColor = mesh.AmbientColor;
                edgeAndFill.Add(new GeometryModel3D(builder.EdgeData.ToMesh(true), edgeMaterial)
                {
                    BackMaterial = edgeMaterial
                });
            }

            return new Model3DGroup
            {
                Children = new Model3DCollection(edgeAndFill)
            };
        }

        private class Edge
        {
            public VertexDirection Direction, PrevDirection, NextDirection;
            public SplitMode Split, PrevSplit, NextSplit;
            public Vector Begin, End;
            public Vector? Prev, Next;

            public void Invert()
            {
                Utils.Swap(ref Begin, ref End);
                Utils.Swap(ref PrevSplit, ref NextSplit);
                Utils.Swap(ref Prev, ref Next);
                Utils.Swap(ref PrevDirection, ref NextDirection);
            }
        }
    }
}