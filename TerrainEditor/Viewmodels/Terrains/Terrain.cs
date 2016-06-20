using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using PropertyTools.DataAnnotations;
using TerrainEditor.Core;
using TerrainEditor.UserControls;
using BrowsableAttribute = System.ComponentModel.BrowsableAttribute;
using CategoryAttribute = PropertyTools.DataAnnotations.CategoryAttribute;

namespace TerrainEditor.Viewmodels.Terrains
{
    public class Terrain  : PropertyChangeBase
    {
        private bool m_isClosed = true;
        private bool m_splitWhenDifferent;

        private int m_smoothFactor = 5;
        private double m_strechThreshold;
        private int m_pixelsPerUnit = 64;

        private FillMode m_fillMode = FillMode.None;
        private InterpolationMode m_interpolationMode = InterpolationMode.Hermite;
        private Color m_ambientColor = Colors.White;
        private Point3D m_position = new Point3D(0,0,1);
        private string m_name = "New Terrain";

        private UvMapping m_uvMapping;

        private Model3DGroup m_meshCache;
        private bool m_isDirty = true;

        [Category("Misc")]
        public string Name
        {
            get { return m_name; }
            set
            {
                if (value == m_name) return;
                m_name = value;
                OnPropertyChanged();
            }
        }
        public Point3D Position
        {
            get { return m_position; }
            set
            {
                if (value.Equals(m_position)) return;
                m_position = value;
                OnPropertyChanged();
            }
        }
        [Category("Terrain Type")]
        public bool SplitWhenDifferent
        {
            get { return m_splitWhenDifferent; }
            set
            {
                if (value == m_splitWhenDifferent) return;
                m_splitWhenDifferent = value;
                OnPropertyChanged();
            }
        }
        public FillMode FillMode
        {
            get { return m_fillMode; }
            set
            {
                if (value == m_fillMode) return;
                m_fillMode = value;
                OnPropertyChanged();
            }
        }
        [SelectorStyle(SelectorStyle.ListBox)]
        public InterpolationMode InterpolationMode
        {
            get { return m_interpolationMode; }
            set
            {
                if (value == m_interpolationMode)
                    return;
                m_interpolationMode = value;
                OnPropertyChanged();
            }
        }
        [Slidable(1,50)]
        public int SmoothFactor
        {
            get { return m_smoothFactor; }
            set
            {
                if (value == m_smoothFactor || value < 1) return;
                m_smoothFactor = value;
                OnPropertyChanged();
            }
        }

        [Category("Visuals")]
        public bool IsClosed
        {
            get { return m_isClosed; }
            set
            {
                if (value == m_isClosed) return;
                m_isClosed = value;
                OnPropertyChanged();
            }
        }
        [Slidable(-1.0, 1.0)]
        [FormatString("0.00")]
        public double StrechThreshold
        {
            get { return m_strechThreshold; }
            set
            {
                if (value.Equals(m_strechThreshold)) return;
                m_strechThreshold = value;
                OnPropertyChanged();
            }
        }
        [Slidable(16, 256)]
        public int PixelsPerUnit
        {
            get { return m_pixelsPerUnit; }
            set
            {
                if (value == m_pixelsPerUnit) return;
                m_pixelsPerUnit = value;
                OnPropertyChanged();
            }
        }
        public Color AmbientColor
        {
            get { return m_ambientColor; }
            set
            {
                if (value.Equals(m_ambientColor)) return;
                m_ambientColor = value;
                OnPropertyChanged();
            }
        }
        [Category("Terrain Data"), SortIndex(5), CustomEditor(typeof(UvMappingPropertyEditor))]
        public UvMapping UvMapping
        {
            get { return m_uvMapping; }
            set
            {
                if (Equals(value, m_uvMapping)) return;
                m_uvMapping = value;
                OnPropertyChanged();
            }
        }
        [Category("Terrain Data"), List(false,true),SortIndex(6)]
        public ObservableCollection<VertexInfo> Vertices { get; }

        public Terrain(IEnumerable<VertexInfo> vertices = null)
        {
            vertices = vertices ?? Enumerable.Empty<VertexInfo>();
            Vertices = new ObservableCollection<VertexInfo>(vertices);

            RecursivePropertyChanged += (sender, args) => m_isDirty = true;
        }

        [Browsable(false)]
        public Model3DGroup Mesh
        {
            get
            {
                if (m_isDirty)
                {
                    m_meshCache = TerrainMeshBuilder.GenerateMesh(this);
                    m_meshCache.Transform = new TranslateTransform3D(Position.X,Position.Y,Position.Z);
                    m_meshCache.SetName(Name);
                    m_isDirty = false;
                }

                return m_meshCache;
            }
        }
    }
}