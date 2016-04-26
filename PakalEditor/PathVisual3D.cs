using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;

namespace PakalEditor
{
    public class PathVisual3D : ModelVisual3D
    {
        public enum HitType
        {
            None,
            Vertex,
            AddNew,
            Direction
        }
        public delegate void HitTest2DDelegate(int index, HitType type);

        static PathVisual3D()
        {
            DotVertexMaterial = Utils.CreateImageMaterial(Utils.LoadBitmapFromResource("Resources/dot.png"));
            DotAddMaterial = Utils.CreateImageMaterial(Utils.LoadBitmapFromResource("Resources/dot-add.png"));
            DotDeleteMaterial = Utils.CreateImageMaterial(Utils.LoadBitmapFromResource("Resources/dot-delete.png"));
            DotAutoMaterial = Utils.CreateImageMaterial(Utils.LoadBitmapFromResource("Resources/dot-auto.png"));

            DiffuseMaterial dotDirMat = Utils.CreateImageMaterial(Utils.LoadBitmapFromResource("Resources/dot-dir.png"), tile: false, freezeBrush: false);
            DotTopMaterial = new DiffuseMaterial((Brush) dotDirMat.Brush.GetCurrentValueAsFrozen());
            
            dotDirMat.Brush.RelativeTransform = new RotateTransform(-90,0.5,0.5);
            DotLeftMaterial = new DiffuseMaterial((Brush)dotDirMat.Brush.GetCurrentValueAsFrozen());

            dotDirMat.Brush.RelativeTransform = new RotateTransform(90, 0.5, 0.5);
            DotRightMaterial = new DiffuseMaterial((Brush)dotDirMat.Brush.GetCurrentValueAsFrozen());

            dotDirMat.Brush.RelativeTransform = new RotateTransform(180, 0.5, 0.5);
            DotDownMaterial = new DiffuseMaterial((Brush)dotDirMat.Brush.GetCurrentValueAsFrozen());

            DotVertexMaterial.Freeze();
            DotAddMaterial.Freeze();
            DotDeleteMaterial.Freeze();
            DotAutoMaterial.Freeze();
            DotTopMaterial.Freeze();
            DotLeftMaterial.Freeze();
            DotRightMaterial.Freeze();
            DotDownMaterial.Freeze();
    }

        public static readonly Material DotVertexMaterial;
        public static readonly Material DotAddMaterial;
        public static readonly Material DotDeleteMaterial;
        public static readonly Material DotAutoMaterial;
        public static readonly Material DotTopMaterial;
        public static readonly Material DotLeftMaterial;
        public static readonly Material DotRightMaterial;
        public static readonly Material DotDownMaterial;

        public IReadOnlyList<VertexInfo> Vertices { get; }

        public Material VertexMaterial { get; set; } = DotVertexMaterial;
        public Material AddNewMaterial { get; set; } = DotAddMaterial;

        public bool ShowDirectionCallout { get; set; } = true;
        public bool ShowCornerCallout { get; set; } = true;
        public bool Closed { get; set; } = false;
        public double StartZOffset { get; set; } = 0;

        private MeshGeometry3D m_mesh;
        private LineGeometryBuilder m_geometry_builder;

        private List<BillboardVisual3D> m_vertices_callouts;
        private List<BillboardVisual3D> m_add_new_callouts = new List<BillboardVisual3D>();
        private List<BillboardVisual3D> m_direction_callouts = new List<BillboardVisual3D>();

        public PathVisual3D(IReadOnlyList<VertexInfo> vertices, Color? color = null )
        {
            Utils.EnsureNotNull(vertices);

            Vertices = vertices;

            m_mesh = new MeshGeometry3D();
            m_geometry_builder = new LineGeometryBuilder(this);

            Material colorMaterial = MaterialHelper.CreateMaterial(color ?? Colors.White);
            Content = new GeometryModel3D { Geometry = m_mesh, Material = colorMaterial, BackMaterial = colorMaterial};
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void Tesellate()
        {
            Children.Clear();

            IEnumerable<VertexInfo> vertices = Closed && Vertices.Count > 0 ? Vertices.Concat(Vertices[0]) : Vertices;

            //Add new callout section
            if (AddNewMaterial != null)
            {
                m_add_new_callouts = vertices.Pairwise((fst, snd) => new {Fst = fst, Snd = snd}).Select(arg => new BillboardVisual3D
                {
                    Position = VectorUtils.LinearLerp(arg.Fst, arg.Snd, 0.5) + new Vector3D(0, StartZOffset - 0.03, 0),
                    Width = 15,
                    Height = 15,
                    Material = AddNewMaterial,
                }).ToList();

                m_add_new_callouts.ForEach(Children.Add);
            }
            //direction callout section
            if (ShowDirectionCallout)
            {
                m_direction_callouts = vertices.Pairwise(Tuple.Create).Select(arg =>
                {
                    Material material = null;

                    switch (arg.Item1.Direction)
                    {
                        case SegmentDirection.Auto:
                            material = DotAutoMaterial;
                            break;
                        case SegmentDirection.Top:
                            material = DotTopMaterial;
                            break;
                        case SegmentDirection.Down:
                            material = DotDownMaterial;
                            break;
                        case SegmentDirection.Left:
                            material = DotLeftMaterial;
                            break;
                        case SegmentDirection.Right:
                            material = DotRightMaterial;
                            break;
                    }

                    return new BillboardVisual3D
                    {
                        Position = VectorUtils.LinearLerp(arg.Item1, arg.Item2, 0.1) + new Vector3D(0, StartZOffset - 0.02, 0),
                        Width = 15,
                        Height = 15,
                        Material = material,
                    };
                }).ToList();
                m_direction_callouts.ForEach(Children.Add);
            }

            //Vertex callout section
            if (VertexMaterial != null)
            {
                m_vertices_callouts = Vertices.Select(vertex => new BillboardVisual3D
                {
                    Position = vertex.Point + new Vector3D(0, StartZOffset - 0.03, 0),
                    Width = 15,
                    Height = 15,
                    Material = VertexMaterial,
                }).ToList();

                for (int i = 0; i < m_vertices_callouts.Count; i++)
                {
                    if (ShowCornerCallout || (i != 0 && i + 1 != m_vertices_callouts.Count))
                    {
                        Children.Add(m_vertices_callouts[i]);
                    }
                }
            }

            //Segment section
            Vector3D offset = new Vector3D(0, StartZOffset - 0.01,0);
            var segments = vertices.Pairwise((fst, snd) => new[] {fst.Point + offset, snd.Point + offset, }).SelectMany(ds => ds).ToList();
            int count = segments.Count;
            if (count > 1)
            {
                if (this.IsAttachedToViewport3D())
                {
                    m_geometry_builder.UpdateTransforms();
                }

                m_mesh.TriangleIndices = m_geometry_builder.CreateIndices(count);
                m_mesh.Positions = m_geometry_builder.CreatePositions(segments);
            }
            else
            {
                m_mesh.TriangleIndices = null;
                m_mesh.Positions = null;
            }

        }
        public void FeedHitTest(Point position, HitTest2DDelegate callBack)
        {
            var callout = this.GetViewport3D().FindNearestVisual(position) as BillboardVisual3D;

            if (callout == null)
            {
                callBack(-1, HitType.None);
                return;
            }

            var index = m_add_new_callouts.IndexOf(callout);

            if (index != -1)
            {
                callBack(index,HitType.AddNew);
                return;
            }

            index = m_vertices_callouts.IndexOf(callout);

            if (index != -1)
            {
                callBack(index,HitType.Vertex);
                return;
            }


            index = m_direction_callouts.IndexOf(callout);

            if (index != -1)
            {
                callBack(index, HitType.Direction);
                return;
            }
        }
    }
}