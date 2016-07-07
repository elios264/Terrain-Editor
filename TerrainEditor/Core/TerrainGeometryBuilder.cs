using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Poly2Tri;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;
using Urho;
using FillMode = TerrainEditor.Viewmodels.Terrains.FillMode;
using InterpolationMode = TerrainEditor.Viewmodels.Terrains.InterpolationMode;
using Terrain = TerrainEditor.Viewmodels.Terrains.Terrain;


namespace TerrainEditor.Core
{
    public class TerrainGeometryBuilder
    {
        private bool IsClosed => Terrain.IsClosed && Terrain.Vertices.Count > 2;
        private bool IsInverted => Terrain.FillMode == FillMode.Inverted && Terrain.Vertices.Count > 2;

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
        public GeometryBuilder EdgeData { get; }
        public GeometryBuilder FillData { get; }

        public TerrainGeometryBuilder()
        {
            FillData = new GeometryBuilder(false,true);
            EdgeData = new GeometryBuilder(false,true);
        }

        public void Tesellate()
        {
            List<Vector2> edgeCentalPoints = null;

            EdgeData.Clear();
            FillData.Clear();

            if (Terrain.UvMapping.EdgeTexture != null && Terrain.UvMapping.Top.Bodies.Count > 0)
            {
                TesellateEdges(out edgeCentalPoints);

                //I'm not sure about the material techqunique to use to avoid this sorting
                var triangleIndices = (List<int>)EdgeData.TriangleIndices;
                var sortedIndices = EdgeData.TriangleIndices
                    .Batch(3)
                    .Select(ints => { var indices = ints.ToArray(); return new { indices, z = EdgeData.Positions[indices[0]].Z }; })
                    .OrderBy(arg => -arg.z)
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
        private void TesellateEdges(out List<Vector2> centralPoints)
        {
            centralPoints = new List<Vector2>();

            var normalStart = (Vector2?)null;
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

                float[] edgesLengths;
                var segmentMapping = SegmentFor(batch[0].Direction);
                var smoothFactor = batch.Length == 1 && batch[0].Prev == null && batch[lst].Next == null ? 1 : Terrain.SmoothFactor;
                var totalLength = InterpolateLength(Terrain.InterpolationMode, batch, smoothFactor * 5, out edgesLengths);
                var bodyCount = (float)Math.Max((int)Math.Round(totalLength / (segmentMapping.BodySize.X / pixelsPerUnit) + Terrain.StrechThreshold), 1);
                var finalBodySize = new Vector2(totalLength / bodyCount, segmentMapping.BodySize.Y / pixelsPerUnit);
                var halfFinalBodySizeHeight = finalBodySize.Y / 2;
                var incLength = finalBodySize.X / smoothFactor;
                var currentLength = incLength;
                var first = true;
                var offsets = segmentMapping.Offsets;
                offsets = new Vector3(offsets.X / pixelsPerUnit,offsets.Y / pixelsPerUnit, offsets.Z / pixelsPerUnit);


                for (int i = 0; i < bodyCount; i++)
                {
                    var bodyUV = Terrain.UvMapping.ToUV(new Rect(segmentMapping.Bodies[Math.Abs(begin.GetHashCode() % segmentMapping.Bodies.Count)], segmentMapping.BodySize));
                    bodyUV.Max.X = bodyUV.Max.X/smoothFactor;

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
                            new Vector3(localBottomLeft.X, localBottomLeft.Y, offsets.Z),
                            new Vector3(localTopLeft.X, localTopLeft.Y, offsets.Z),
                            new Vector3(localTopRight.X, localTopRight.Y, offsets.Z),
                            new Vector3(localBottomRight.X, localBottomRight.Y, offsets.Z),
                            bodyUV.BottomLeft(), bodyUV.TopLeft(), bodyUV.TopRight(), bodyUV.BottomRight());

                        if (first)
                        {
                            first = TesellateCap(new Rect(segmentMapping.LeftCap, segmentMapping.CapSize), true, batch[0], normalStart ?? normalEnd, offsets);
                            firstCapDone = firstCapDone ?? first;
                            first = false;
                        }

                        centralPoints.Add(begin);
                        normalStart = normalEnd;
                        bodyUV.Min.X = bodyUV.Min.X + bodyUV.Max.X;
                        begin = end;
                    }
                }
                TesellateCap(new Rect(segmentMapping.RightCap, segmentMapping.CapSize), false, batch[lst], normalStart.Value, offsets);
            }
        }
        private bool TesellateCap(Rect area, bool left, Edge edge, Vector2 verticalNormal, Vector3 offset)
        {
            if (!ShouldCloseEdge(edge, left) || area.Equals(default(Rect)))
                return false;

            var capUv = Terrain.UvMapping.ToUV(area);
            var capSize = new Vector2(area.Max.X / Terrain.PixelsPerUnit, area.Max.Y / Terrain.PixelsPerUnit);

            var top = (left ? edge.Begin : edge.End) + verticalNormal * (capSize.Y / 2) + verticalNormal * offset.Y;
            var bottom = (left ? edge.Begin : edge.End) - verticalNormal * (capSize.Y / 2) + verticalNormal * offset.Y;

            var horizontalNormal = (bottom - top).Normal();

            top += horizontalNormal * offset.X * (left ? -1 : 1);
            bottom += horizontalNormal * offset.X * (left ? -1 : 1);

            var capOffset = horizontalNormal * capSize.X;
            var otherTop = left ? top - capOffset : top + capOffset ;
            var otherBottom = left ? bottom - capOffset : bottom + capOffset ;

            if (left)
            {
                Utils.Swap(ref top, ref otherTop);
                Utils.Swap(ref bottom, ref otherBottom);
            }

            EdgeData.AddQuad(
                new Vector3(bottom.X, bottom.Y, offset.Z),
                new Vector3(top.X, top.Y, offset.Z),
                new Vector3(otherTop.X, otherTop.Y, offset.Z),
                new Vector3(otherBottom.X, otherBottom.Y, offset.Z),
                capUv.BottomLeft(), capUv.TopLeft(), capUv.TopRight(), capUv.BottomRight());

            return true;
        }
        private void TesellateFill(List<Vector2> edgeVertices)
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
                var centroid = polygon.GetCentroid();
                var center = new Vector2(centroid.Xf,centroid.Yf);
                var size = new Vector2((float) polygon.BoundingBox.Width,(float) polygon.BoundingBox.Height);

                var topLeft = new Vector2(center.X - size.X, center.Y + size.Y);
                var topRight = new Vector2(center.X + size.X, center.Y + size.Y);
                var bottomLeft = new Vector2(center.X - size.X, center.Y - size.Y);
                var bottomRight = new Vector2(center.X + size.X, center.Y - size.Y);

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

            var unitsPerFill = new Vector2(
                Terrain.UvMapping.FillTexture.PixelWidth / (float)Terrain.PixelsPerUnit,
                Terrain.UvMapping.FillTexture.PixelHeight / (float)Terrain.PixelsPerUnit);
            foreach (var triangle in polygon.Triangles)
            {
                FillData.AddTriangle(
                    new Vector3(triangle.Points._2.Xf, triangle.Points._2.Yf,0f),
                    new Vector3(triangle.Points._1.Xf, triangle.Points._1.Yf,0f),
                    new Vector3(triangle.Points._0.Xf, triangle.Points._0.Yf,0f),
                    new Vector2(triangle.Points._2.Xf / unitsPerFill.X, 1 - triangle.Points._2.Yf / unitsPerFill.Y),
                    new Vector2(triangle.Points._1.Xf / unitsPerFill.X, 1 - triangle.Points._1.Yf / unitsPerFill.Y),
                    new Vector2(triangle.Points._0.Xf / unitsPerFill.X, 1 - triangle.Points._0.Yf / unitsPerFill.Y));
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
        private static Vector2 Interpolate(InterpolationMode mode ,Edge[] edges, float[] edgesLengths, float length)
        {
            var i = 0;
            var percentaje = 1.0f;
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

            return Interpolate(mode, edges[i].Prev ?? edges[i].Begin, edges[i].Begin, edges[i].End,
                edges[i].Next ?? edges[i].End, percentaje);
        }
        private static Vector2 Interpolate(InterpolationMode mode ,Vector2 a, Vector2 b, Vector2 c, Vector2 d, float percentaje)
        {
            switch (mode)
            {
            case InterpolationMode.Hermite: return Utils.HermiteInterpolate(a, b, c, d, percentaje);
            case InterpolationMode.Cubic: return Utils.CubicInterpolate(a, b, c, d, percentaje);
            default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
        private static float InterpolateLength(InterpolationMode mode,IEnumerable<Edge> edges, int resolution, out float[] partialLengths)
        {
            partialLengths = edges
                .Select(e => Enumerable
                    .Range(0, resolution + 1)
                    .Select(cur => Interpolate(mode ,e.Prev ?? e.Begin, e.Begin, e.End, e.Next ?? e.End, 1.0f / resolution * cur))
                    .Pairwise((v1, v2) => (v2 - v1).Length)
                    .Sum())
                .ToArray();

            return partialLengths.Sum();
        }

        public static Node GenerateMeshNode(Terrain terrain)
        {
            if (terrain.UvMapping == null)
                return new Node();

            var builder = new TerrainGeometryBuilder { Terrain = terrain };
            builder.Tesellate();

            BoundingBox box;
            Model model = new Model();
            Vector3 geometryCenter = terrain.Centroid;

            if (builder.Terrain.FillMode != FillMode.None)
            {
                model.NumGeometries = 1;
                model.SetGeometry(0, 0, builder.FillData.ToGeometry(out box));
                model.SetGeometryCenter(0, geometryCenter);
                model.BoundingBox = box;
            }

            if (terrain.UvMapping.EdgeTexture != null)
            {
                model.NumGeometries++;
                model.SetGeometry(model.NumGeometries - 1, 0, builder.EdgeData.ToGeometry(out box));
                model.SetGeometryCenter(model.NumGeometries - 1, geometryCenter);
                model.BoundingBox = box;
            }

            var node = new Node {Name = terrain.Name };
            var staticModel  = node.CreateComponent<StaticModel>();
            staticModel.Model = model;

            if (builder.Terrain.FillMode != FillMode.None)
            {
                var mat = new Material();
                mat.SetTexture(TextureUnit.Diffuse, Application.Current.ResourceCache.GetTexture2D(terrain.UvMapping.FillTexturePath));
                mat.SetTechnique(0, CoreAssets.Techniques.DiffAlpha, 1, 1);
                mat.SetShaderParameter("MatDiffColor", terrain.AmbientColor.ToUrhoColor());
                staticModel.SetMaterial(0, mat);
            }

            if (terrain.UvMapping.EdgeTexture != null)
            {
                var mat = new Material();
                mat.SetTexture(TextureUnit.Diffuse, Application.Current.ResourceCache.GetTexture2D(terrain.UvMapping.EdgeTexturePath));
                mat.SetTechnique(0, CoreAssets.Techniques.DiffAlpha, 1, 1);
                mat.SetShaderParameter("MatDiffColor", terrain.AmbientColor.ToUrhoColor());

                staticModel.SetMaterial(model.NumGeometries - 1, mat);
            }

            node.Translate(terrain.Position,TransformSpace.Local);
            node.Rotate(new Quaternion(0, 0, terrain.ZRotation), TransformSpace.Local);

            return node;
        }

        private class Edge
        {
            public VertexDirection Direction, PrevDirection, NextDirection;
            public SplitMode Split, PrevSplit, NextSplit;
            public Vector2 Begin, End;
            public Vector2? Prev, Next;

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