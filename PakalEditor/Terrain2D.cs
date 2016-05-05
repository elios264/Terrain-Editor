using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;
using Poly2Tri;
using Strilanc.Value;
using Point = System.Windows.Point;
using Polygon = Poly2Tri.Polygon;

namespace PakalEditor
{
    public enum SegmentDirection
    {
        Auto,
        Top,
        Right,
        Down,
        Left
    }
    public struct VertexInfo
    {
        public SegmentDirection Direction;
        public Point3D Point;

        public VertexInfo(Point3D point, SegmentDirection direction = SegmentDirection.Auto)
        {
            Direction = direction;
            Point = point;
        }

        public VertexInfo(double x, double y, double z, SegmentDirection direction = SegmentDirection.Auto) : this(new Point3D(x, y, z), direction) { }

        public static implicit operator VertexInfo(Point3D point)
        {
            return new VertexInfo(point);
        }
        public static implicit operator Point3D(VertexInfo vertexInfo)
        {
            return vertexInfo.Point;
        }
    }

    public class Terrain2DVisual : ModelVisual3D
    {
        private struct PreviewInfo
        {
            public int InsertPosition;
            public Point3D Point;
        }
        private class Segment
        {
            public SegmentDirection Direction;

            public May<SegmentDirection> PrevDirection;
            public May<SegmentDirection> NextDirection;

            public Point3D Begin;
            public Point3D End;

            public May<Point3D> PrevPrev;
            public May<Point3D> Prev;
            public May<Point3D> Next;
            public May<Point3D> NextNext;

            public double AngleWithPrev
            {
                get
                {
                    var begin = Begin;
                    var end = End;

                    return Prev.Match(
                        p =>
                        {
                            double angle = Vector.AngleBetween((end - begin).ToVectorXZ(), (p - begin).ToVectorXZ());
                            return angle < 0 ? angle + 360 : angle;
                        }, 180);
                }
            }
            public double AngleWithNext
            {
                get
                {
                    var begin = Begin;
                    var end = End;

                    return Next.Match(
                        n =>
                        {
                            double angle = Vector.AngleBetween((begin - end).ToVectorXZ(), (n - end).ToVectorXZ());
                            return angle < 0 ? angle + 360 : angle;
                        }, 180);
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
        private enum SegmentSide
        {
            Left, Right
        }
        public enum FillMode
        {
            None,
            Fill,
            Inverted,
        }

        private static readonly SegmentDirection[] Directions = Enum.GetValues(typeof(SegmentDirection)).Cast<SegmentDirection>().ToArray();

        //Paths
        private List<VertexInfo> m_preview;
        private List<VertexInfo> m_wireframe;
        private ObservableCollection<VertexInfo> m_path;

        //Visuals
        private PathVisual3D m_visual_path;
        private PathVisual3D m_visual_preview;
        private PathVisual3D m_visual_wireframe;
        private ModelVisual3D m_visual_fill;
        private ModelVisual3D m_visual_edges;

        //miscelaneous helpers
        private Plane3D m_terrain_plane;
        private bool IsInverted
        {
            get { return Mode == FillMode.Inverted && m_path.Count > 2; }
        }

        //Path helpers
        private int m_current_vertex = -1;
        private Point m_last_point = default(Point);
        private ModifierKeys m_modifier_keys = ModifierKeys.None;
        private PreviewInfo m_preview_info = new PreviewInfo {InsertPosition =  - 1};

        //Mesh helpers
        private MeshBuilder m_builder = new MeshBuilder(false, true);
        private List<Point3D> m_fill_vertices = new List<Point3D>();
        private Size m_units_per_edge_uv;
        private Size m_units_per_fill_uv;
        private uint m_pixels_per_unit;
        private DiffuseMaterial m_edge_material;
        private DiffuseMaterial m_fill_material;
        private TerrainTexture m_texture;

        //Terrain properties
        public TerrainTexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                m_texture = value;
                m_edge_material = Utils.CreateImageMaterial(image: value.EdgeTexture);
                m_fill_material = Utils.CreateImageMaterial(image: value.FillTexture, tile: true);
                PixelsPerUnit = PixelsPerUnit;
                m_visual_path.StartZOffset = m_visual_preview.StartZOffset = m_visual_wireframe.StartZOffset = value.FindFrontestOffset();
            }
        }
        public uint PixelsPerUnit
        {
            get { return m_pixels_per_unit; }
            set
            {
                m_pixels_per_unit = value;
                if (Texture?.EdgeTexture != null)
                {
                    m_units_per_edge_uv = new Size(Texture.EdgeTexture.PixelWidth/(double) PixelsPerUnit,
                        Texture.EdgeTexture.PixelHeight/(double) PixelsPerUnit);
                }

                if (Texture?.FillTexture != null)
                {
                    m_units_per_fill_uv = new Size(Texture.FillTexture.PixelWidth/(double) PixelsPerUnit,
                        Texture.FillTexture.PixelHeight/(double) PixelsPerUnit);
                }

            }
        }

        public bool Closed { get; set; } = true;
        public bool ShowWireFrame { get; set; } = false;
        public int SmoothFactor { get; set; } = 5;
        public Color AmbientColor { get; set; } = Colors.White;
        public double StrechThreshold { get; set; } = 0.5;
        public int  SplitCornersThreshold { get; set; } = 90;
        public bool SplitWhenDifferent { get; set; } = false;
        public FillMode Mode { get; set; } = FillMode.Fill;

        public Terrain2DVisual(Viewport3D viewport)
        {
            //Set the transform
            var camera = (ProjectionCamera) viewport.Camera;

            var ax1 = camera.LookDirection.Normalized();
            var ax2 = Vector3D.CrossProduct(ax1, camera.UpDirection).Normalized() * -1;
            var ax3 = camera.UpDirection.Normalized();

            var translation = (ax1*40 + camera.Position).ToVector3D();
            var rotation = new Quaternion(ax3, -90) * QuaternionUtils.CreateFromRotationAxes(ax1,ax2,ax3);

            var transformGroup = new Transform3DGroup();

            transformGroup.Children.Add(new RotateTransform3D(new QuaternionRotation3D(rotation)));
            transformGroup.Children.Add(new TranslateTransform3D(translation));

            Transform = transformGroup;

            //Set the plane
            m_terrain_plane = new Plane3D(translation.ToPoint3D(),camera.LookDirection.Normalized());

            //Set the mesh
            m_path = new ObservableCollection<VertexInfo> { new VertexInfo(-5, 0, 5), new VertexInfo(5, 0, 5), new VertexInfo(5, 0, -4) , new VertexInfo(-5,0,-4) };
            m_preview = new List<VertexInfo>(3);
            m_wireframe = new List<VertexInfo>(6);

            m_path.CollectionChanged += (sender, args) => Tesellate();

            m_visual_path = new PathVisual3D(m_path,Colors.White);
            m_visual_preview = new PathVisual3D(m_preview,Colors.PaleGreen) { AddNewMaterial = null, ShowCornerCallout = false, ShowDirectionCallout = false};
            m_visual_wireframe = new PathVisual3D(m_wireframe,Colors.Black) { AddNewMaterial = null, VertexMaterial = null, ShowDirectionCallout =  false};
            m_visual_fill = new ModelVisual3D();
            m_visual_edges = new ModelVisual3D();


            Children.Add(m_visual_fill);
            Children.Add(m_visual_edges);
            Children.Add(m_visual_wireframe);
            Children.Add(m_visual_path);
            Children.Add(m_visual_preview);

            //Intialize properties
            Texture = TerrainTexture.Mossy;
            PixelsPerUnit = 96;
        }

        public void StartManipulation(Point point2D)
        {
            switch (m_modifier_keys)
            {
                case ModifierKeys.None:
                    m_visual_path.FeedHitTest(point2D, (index, type) =>
                    {
                        switch (type)
                        {
                            case PathVisual3D.HitType.AddNew:
                                m_path.Insert(index + 1, new VertexInfo(Point2DTo3D(point2D),m_path[index].Direction)); break;
                            case PathVisual3D.HitType.Vertex:
                                m_current_vertex = index; break;
                            case PathVisual3D.HitType.Direction:
                                VertexInfo vertex = m_path[index];
                                vertex.Direction = Directions.At(Array.IndexOf(Directions, m_path[index].Direction) + 1, true).ForceGetValue();
                                m_path[index] = vertex;
                            break;
                        }
                    });
                    break;
                case ModifierKeys.Control:
                    m_visual_path.FeedHitTest(point2D, (index, type) =>
                    {
                        if (type == PathVisual3D.HitType.Vertex)
                        {
                            m_path.RemoveAt(index);
                        }
                    });

                    if (m_path.Count < 3)
                    {
                        ModificerDeactivated(m_modifier_keys & ~ModifierKeys.Control);
                    }
                    break;
                case ModifierKeys.Shift:
                    if (m_preview_info.InsertPosition != -1)
                    {
                        int index = m_preview_info.InsertPosition;
                        m_path.Insert(index,new VertexInfo(m_preview_info.Point, index == 0 ? SegmentDirection.Auto : m_path[index-1].Direction));
                        m_preview.Clear();
                        m_visual_preview.Tesellate();
                        m_preview_info.InsertPosition = -1;
                    }
                    break;
            }
        }
        public void DeltaManipulation(Point point2D)
        {
            m_last_point = point2D;

            if (m_current_vertex != -1)
            {
                VertexInfo vertex = m_path[m_current_vertex];

                vertex.Point = Point2DTo3D(point2D);
                m_path[m_current_vertex] = vertex;
            }

            if (m_modifier_keys == ModifierKeys.Shift)
            {
                var position3D = Point2DTo3D(point2D);

                var closestPoints =
                    m_path.Select((p, i) => new { Distance = position3D.DistanceToSquared(p), Index = i, Point = p })
                        .OrderBy(arg => arg.Distance)
                        .ToArray();


                var closest = closestPoints[0];
                var secondClosest = closestPoints.First(arg => Math.Abs(arg.Index - closest.Index) == 1);

                var vectorA = point2D - Point3DTo2D(closest.Point);
                var vectorB = Point3DTo2D(secondClosest.Point) - Point3DTo2D(closest.Point);
                var dotProcuct = vectorA.Normalized() * vectorB.Normalized();

                m_preview.Clear();

                if ((closest.Index == 0 || closest.Index == m_path.Count - 1) && dotProcuct < 0.5)
                {
                    m_visual_preview.ShowCornerCallout = true;
                    m_preview.Add(position3D);
                    m_preview.Add(m_path[closest.Index]);

                    m_preview_info.Point = position3D;
                    m_preview_info.InsertPosition = closest.Index == 0 ? 0 : closest.Index + 1;
                }
                else
                {
                    m_visual_preview.ShowCornerCallout = false;
                    m_preview.AddRange(new VertexInfo[]
                    {
                        closest.Point,
                        position3D,
                        secondClosest.Point
                    });
                    m_preview_info.InsertPosition = closest.Index > secondClosest.Index ? closest.Index : secondClosest.Index;
                    m_preview_info.Point = position3D;
                }

                m_visual_preview.Tesellate();
            }

        }
        public void EndManipulation(Point point2D)
        {
            m_current_vertex = -1;
        }

        public void ModifierActivated(ModifierKeys modifiers)
        {
            if (modifiers == ModifierKeys.Control)
            {
                if (m_path.Count < 3)
                {
                    return;
                }

                if (m_visual_path.VertexMaterial == PathVisual3D.DotVertexMaterial)
                {
                    m_visual_path.VertexMaterial = PathVisual3D.DotDeleteMaterial;
                    m_visual_path.Tesellate();
                }
            }
            if (modifiers == ModifierKeys.Shift)
            {
                if (m_preview_info.InsertPosition == -1)
                {
                    DeltaManipulation(m_last_point);
                }
            }

            m_modifier_keys = modifiers;
        }
        public void ModificerDeactivated(ModifierKeys modifiers)
        {
            if (!modifiers.HasFlag(ModifierKeys.Control))
            {
                if (m_visual_path.VertexMaterial == PathVisual3D.DotDeleteMaterial)
                {
                    m_visual_path.VertexMaterial = PathVisual3D.DotVertexMaterial;
                    m_visual_path.Tesellate();
                }
            }

            if (!modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (m_preview.Count > 0)
                {
                    m_preview.Clear();
                    m_visual_preview.Tesellate();
                    m_preview_info.InsertPosition = -1;
                }
            }

            m_modifier_keys = modifiers;
        }

        public void Tesellate()
        {
            //Draw the vertices
            m_visual_path.Closed = Closed && m_path.Count > 2;
            m_visual_path.Tesellate();

            //Build the mesh
            if (m_edge_material != null)
            {

                //Add the bodies
                foreach (Segment segment in Segments())
                {
                    List<Point3D> fillPoints = SmoothFactor > 0 ? DrawSmoothSegment(segment) : DrawRegularSegment(segment);

                    if (IsInverted)
                        fillPoints.Reverse();

                    m_fill_vertices.AddRange(fillPoints);
                }

                //sort the triangles because of the stupid wpf
                List<int> newIndices =
                    m_builder.TriangleIndices.Batch(3)
                        .Select(ints =>
                        {
                            int[] indices = ints.ToArray();
                            return new {vertices = indices, z = m_builder.Positions[indices[0]].Y};
                        })
                        .OrderByDescending(arg => arg.z)
                        .SelectMany(arg => arg.vertices)
                        .ToList();

                m_builder.TriangleIndices.Clear();
                newIndices.ForEach(m_builder.TriangleIndices.Add);


                //draw the mesh
                m_edge_material.AmbientColor = AmbientColor;
                m_visual_edges.Content = new GeometryModel3D
                {
                    Material = m_edge_material,
                    BackMaterial = m_edge_material,
                    Geometry = m_builder.ToMesh(true),
                };

                //clear the builder
                m_builder.Positions.Clear();
                m_builder.TextureCoordinates.Clear();
                m_builder.TriangleIndices.Clear();
            }
            else
            {
                m_visual_edges.Content = null;
            }

            //Draw the fill
            DrawFill();

            //draw & clear the wireframe
            m_visual_wireframe.Tesellate();
            m_wireframe.Clear();
        }

        private List<Point3D> DrawRegularSegment(Segment segment)
        {
            var rect = TextureRectOf(segment.Direction);
            if (!rect.HasValue)
            {
                return new List<Point3D>(new[] { segment.Begin, segment.End });
            }

            var segmentRect = rect.ForceGetValue();
            bool doLeftCap = ShouldCloseSegment(segment, SegmentSide.Left);
            bool doRightCap = ShouldCloseSegment(segment, SegmentSide.Right);

            Size bodyUVSize = Texture.ToUV(segmentRect.Bodies[0]).Size;
            double bodyWidthInUnits = bodyUVSize.Width * m_units_per_edge_uv.Width;
            double bodyHeightInUnits = bodyUVSize.Height * m_units_per_edge_uv.Height;

            double angleWithPrev = doLeftCap ? 180 : segment.AngleWithPrev;
            double angleWithNext = doRightCap ? 180 : segment.AngleWithNext;

            Vector3D startOffset = VectorUtils.RotateVectorXZ(segment.Begin - segment.End, angleWithPrev / 2).Normalized() * (bodyHeightInUnits / 2);
            Vector3D endOffset   = VectorUtils.RotateVectorXZ(segment.Begin - segment.End, angleWithNext / 2).Normalized() * (bodyHeightInUnits / 2);

            Point3D topLeft    = segment.Begin - startOffset;
            Point3D bottomLeft = segment.Begin + startOffset;

            Point3D topRight    = segment.End - endOffset;
            Point3D bottomRight = segment.End + endOffset;
            
            if (doLeftCap)
                DrawCap(segmentRect.LeftCap, SegmentSide.Left, topLeft, bottomLeft,segmentRect.ZOffset);

            if (doRightCap)
                DrawCap(segmentRect.RightCap, SegmentSide.Right, topRight, bottomRight, segmentRect.ZOffset);

            
            int numberOfCuts = Math.Max((int)Math.Floor(segment.Begin.DistanceTo(segment.End) / (bodyWidthInUnits + StrechThreshold)), 1);
            for (var i = 0; i < numberOfCuts; i++)
            {
                double percentStart = i / (double)numberOfCuts;
                double percentEnd = (i + 1) / (double)numberOfCuts;

                Point3D localTopLeft = VectorUtils.LinearLerp(topLeft, topRight, percentStart);
                Point3D localBottomLeft = VectorUtils.LinearLerp(bottomLeft, bottomRight, percentStart);

                Point3D localTopRight = VectorUtils.LinearLerp(topLeft, topRight, percentEnd);
                Point3D localBottomRight = VectorUtils.LinearLerp(bottomLeft, bottomRight, percentEnd);

                Rect bodyUV = Texture.ToUV(segmentRect.Bodies[Math.Abs(percentEnd.GetHashCode() % segmentRect.Bodies.Count)]);
                Vector3D zOffset = new Vector3D(0, segmentRect.ZOffset, 0);

                m_builder.AddQuad(localBottomLeft + zOffset,   localTopLeft + zOffset,   localTopRight + zOffset,   localBottomRight + zOffset,
                                  bodyUV.BottomLeft, bodyUV.TopLeft, bodyUV.TopRight, bodyUV.BottomRight);

                if (ShowWireFrame)
                    m_wireframe.AddRange(new VertexInfo[] { localBottomLeft, localTopRight, localBottomRight, localBottomLeft, localTopLeft, localTopRight });
            }

            return new List<Point3D>(new[] { segment.Begin, segment.End });
        }
        private List<Point3D> DrawSmoothSegment(Segment segment)
        {
            var rect = TextureRectOf(segment.Direction);
            if (!rect.HasValue)
            {
                return new List<Point3D>(new [] { segment.Begin, segment.End });
            }

            TerrainTexture.Segment segmentRect = rect.ForceGetValue();
            Size bodyUVSize = Texture.ToUV(segmentRect.Bodies[0]).Size;
            double bodyWidthInUnits = bodyUVSize.Width * m_units_per_edge_uv.Width;
            double halfBodyHeightInUnits = (bodyUVSize.Height * m_units_per_edge_uv.Height) / 2;

            var bodyUV = Rect.Empty;
            Point3D start = segment.Begin;
            int smoothFactor = SmoothFactor;

            bool doLeftCap = ShouldCloseSegment(segment, SegmentSide.Left);
            bool doRightCap = ShouldCloseSegment(segment, SegmentSide.Right);

            if (doLeftCap)
                segment.PrevPrev = segment.Prev = May.NoValue;

            if (doRightCap)
                segment.NextNext = segment.Next = May.NoValue;

            //check if the previous segment used the a cap
            if (segment.PrevPrev.HasValue && segment.Prev.HasValue && ShouldCloseSegment( new Segment {Prev = segment.PrevPrev, Begin = segment.Prev.ForceGetValue(), End = segment.Begin,}, SegmentSide.Left))
                segment.PrevPrev = May.NoValue;

            //Here I need to calculate the last position of the previous interpolation
            var prevNumOfCuts = (double)Math.Max( (int) Math.Floor(segment.Prev.Else(segment.Begin).DistanceTo(segment.Begin)/ (bodyWidthInUnits + StrechThreshold)), 1)* smoothFactor;
            var endPrevious = VectorUtils.HermiteLerp(segment.PrevPrev.Else(segment.Prev.Else(segment.Begin)), segment.Prev.Else(segment.Begin), segment.Begin, segment.End, prevNumOfCuts == 1 ? 0.001 : ((prevNumOfCuts - 1) / prevNumOfCuts) );
            var startOffset = VectorUtils.NormalXZ(start, endPrevious) * halfBodyHeightInUnits;

            if (doLeftCap)
                DrawCap(segmentRect.LeftCap, SegmentSide.Left, segment.Begin + startOffset, segment.Begin - startOffset, segmentRect.ZOffset);

            if (doLeftCap && doRightCap) //do not waste triangles if the segment is going to be straight
                smoothFactor = 1;

            int numberOfCuts = Math.Max((int)Math.Floor(segment.Begin.DistanceTo(segment.End) / (bodyWidthInUnits + StrechThreshold)), 1) * smoothFactor;
            var fillVertices = new List<Point3D>(numberOfCuts);

            for (var i = 0; i < numberOfCuts; i++)
            {
                double percentEnd = (i + 1)/(double) numberOfCuts;

                Point3D end = VectorUtils.HermiteLerp(segment.Prev.Else(segment.Begin), segment.Begin, segment.End, segment.Next.Else(segment.End), percentEnd);
                Vector3D endOffset = VectorUtils.NormalXZ(end, start)*halfBodyHeightInUnits;

                Point3D localTopLeft = start + startOffset;
                Point3D localBottomLeft = start - startOffset;

                Point3D localTopRight = end + endOffset;
                Point3D localBottomRight = end - endOffset;

                fillVertices.Add(start);
                start = end;
                startOffset = endOffset;

                if (i%smoothFactor == 0)
                {
                    bodyUV = Texture.ToUV(segmentRect.Bodies[Math.Abs(percentEnd.GetHashCode()%segmentRect.Bodies.Count)]);
                    bodyUV.Width /= smoothFactor;
                }
                else
                    bodyUV.X += bodyUV.Width;

                var zOffset = new Vector3D(0,segmentRect.ZOffset,0);
                m_builder.AddQuad(localBottomLeft + zOffset,   localTopLeft + zOffset,   localTopRight + zOffset,   localBottomRight + zOffset,
                                  bodyUV.BottomLeft, bodyUV.TopLeft, bodyUV.TopRight, bodyUV.BottomRight);


                if (ShowWireFrame)
                    m_wireframe.AddRange(new VertexInfo[] {localBottomLeft, localTopRight, localBottomRight, localBottomLeft, localTopLeft, localTopRight});
            }

            if (doRightCap)
                DrawCap(segmentRect.RightCap, SegmentSide.Right, segment.End + startOffset, segment.End - startOffset, segmentRect.ZOffset);


            return fillVertices;
        }

        private void DrawFill()
        {
            if (m_path.Count <= 2 || Mode == FillMode.None || m_fill_material == null)
            {
                m_fill_vertices.Clear();
                m_visual_fill.Content = null;
                return;
            }

            if (Closed == false)
            {
                m_fill_vertices.Add(Mode != FillMode.Inverted ? m_path.Last() : m_path.First());
            }
                

            var polygon = new Polygon(m_fill_vertices.Select(d => new PolygonPoint(d.X, d.Z)));

            if (IsInverted)
            {
                var center = polygon.GetCentroid();
                var size = new Size(polygon.BoundingBox.Width,polygon.BoundingBox.Height) ;

                var topLeft = new Point(center.X - size.Width,center.Y + size.Height);
                var topRight = new Point(center.X + size.Width,center.Y + size.Height);
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
   
            var builder = new MeshBuilder(false,true);

            foreach (var triangle in polygon.Triangles)
            {
                builder.AddTriangle(
                    new Point3D(triangle.Points._0.X, 0.0, triangle.Points._0.Y),
                    new Point3D(triangle.Points._1.X, 0.0, triangle.Points._1.Y),
                    new Point3D(triangle.Points._2.X, 0.0, triangle.Points._2.Y),
                    new Point(triangle.Points._0.X / m_units_per_fill_uv.Width, triangle.Points._0.Y / m_units_per_fill_uv.Height),
                    new Point(triangle.Points._1.X / m_units_per_fill_uv.Width, triangle.Points._1.Y / m_units_per_fill_uv.Height),
                    new Point(triangle.Points._2.X / m_units_per_fill_uv.Width, triangle.Points._2.Y / m_units_per_fill_uv.Height));
            }

            m_fill_vertices.Clear();

            m_fill_material.AmbientColor = AmbientColor;
            m_visual_fill.Content = new GeometryModel3D
            {
                Material = m_fill_material,
                BackMaterial = m_fill_material,
                Geometry = builder.ToMesh(true)
            };
        }
        private void DrawCap(Rect rect, SegmentSide dir, Point3D top, Point3D bottom, double zOffset)
        {
            var capUV = Texture.ToUV(rect);
            var capOffset = VectorUtils.NormalXZ(bottom, top)*capUV.Width*m_units_per_edge_uv.Width;

            var otherTop = dir == SegmentSide.Left ? top - capOffset : top + capOffset;
            var otherBottom = dir == SegmentSide.Left ? bottom - capOffset : bottom + capOffset;

            if (dir == SegmentSide.Left)
            {
                Utils.Swap(ref top, ref otherTop);
                Utils.Swap(ref bottom, ref otherBottom);
            }

            Vector3D offset = new Vector3D(0, zOffset,0);
            m_builder.AddQuad(bottom + offset, top + offset, otherTop + offset, otherBottom + offset,
                              capUV.BottomLeft, capUV.TopLeft, capUV.TopRight, capUV.BottomRight);
        }
        private bool ShouldCloseSegment(Segment segment, SegmentSide dir)
        {
            if (IsInverted)
            {
                dir = dir == SegmentSide.Left ? SegmentSide.Right : SegmentSide.Left;
            }

            //Check if the segments are different
            if (SplitWhenDifferent && (dir == SegmentSide.Left && segment.Direction != segment.PrevDirection || (dir == SegmentSide.Right && segment.Direction != segment.NextDirection)))
            {
                return true;
            }

            double angle = dir == SegmentSide.Left ? segment.AngleWithPrev : segment.AngleWithNext;

            //Check if the angle between the segments breaks the threshold
            if (angle < SplitCornersThreshold || angle > (360 - SplitCornersThreshold))
            {
                return true;
            }
            
            //Checks if is beginning or end of the path
            return angle == 180 && !(dir == SegmentSide.Left ? segment.Prev.HasValue : segment.Next.HasValue);
        }
        private Point3D Point2DTo3D(Point point)
        {
            Point3D worldCoordinatesPoint = this.GetViewport3D().UnProject(point, m_terrain_plane.Position, m_terrain_plane.Normal).Value;
            Point3D terrainCoordinatesPoint = (Transform.Inverse?.Transform(worldCoordinatesPoint)).Value;

            return new Point3D(Math.Round(terrainCoordinatesPoint.X,5), Math.Round(terrainCoordinatesPoint.Y,5), Math.Round(terrainCoordinatesPoint.Z,5));
        }
        private Point Point3DTo2D(Point3D point)
        {  
            return TransformToAncestor(this.GetViewport3D()).Transform(point);
        }
        private May<TerrainTexture.Segment> TextureRectOf(SegmentDirection direction)
        {
            May<TerrainTexture.Segment> segmentInfo;

            switch (direction)
            {
                case SegmentDirection.Top:
                    segmentInfo = Texture.Top;
                    break;
                case SegmentDirection.Down:
                    segmentInfo = Texture.Bottom.Else(Texture.Top);
                    break;
                case SegmentDirection.Left:
                    segmentInfo = Texture.Left.Else(Texture.Right).Else(Texture.Top);
                    break;
                case SegmentDirection.Right:
                    segmentInfo = Texture.Right.Else(Texture.Left).Else(Texture.Top);
                    break;
                default:
                    segmentInfo = May.NoValue;
                    break;
            }

            return segmentInfo.Match( segment => segment.Valid ? segment : May<TerrainTexture.Segment>.NoValue,segmentInfo);
        }
        private SegmentDirection SegmentDirectionOf(Tuple<VertexInfo, VertexInfo> segment)
        {
            if (segment.Item1.Direction != SegmentDirection.Auto)
                return segment.Item1.Direction;

            Vector3D normal = VectorUtils.NormalXZ(segment.Item1.Point, segment.Item2.Point);

            if (Math.Abs(normal.X) > Math.Abs(normal.Z))
            {
                return normal.X < 0
                    ? (IsInverted ? SegmentDirection.Left : SegmentDirection.Right)
                    : (IsInverted ? SegmentDirection.Right : SegmentDirection.Left);
            }

            return normal.Z < 0
                ? (IsInverted ? SegmentDirection.Down : SegmentDirection.Top)
                : (IsInverted ? SegmentDirection.Top : SegmentDirection.Down);
        }
        private List<Segment> Segments()
        {
            int index = -1;
            bool isClosed = Closed && m_path.Count > 2;

            var segments = new List<Segment>(m_path.Count);
            var vertices = isClosed ? m_path.Concat(m_path[0]) : m_path;

            foreach (var pair in vertices.Pairwise(Tuple.Create))
            {
                index++;

                Segment segment = new Segment
                {
                    PrevPrev = m_path.At(index - 2, isClosed).Select(info => info.Point),
                    Prev = m_path.At(index - 1, isClosed).Select(info => info.Point),
                    Begin = pair.Item1.Point,
                    End = pair.Item2.Point,
                    Next = m_path.At(index + 2, isClosed).Select(info => info.Point),
                    NextNext = m_path.At(index + 3, isClosed).Select(info => info.Point),
                    Direction = SegmentDirectionOf(pair),
                };

                if (IsInverted)
                    segment.Invert();

                segments.Add(segment);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].PrevDirection = segments.At(i - 1, isClosed).Select(segment => segment.Direction);
                segments[i].NextDirection = segments.At(i + 1, isClosed).Select(segment => segment.Direction);
            }

            return segments;
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            Tesellate();
        }
    }
}
