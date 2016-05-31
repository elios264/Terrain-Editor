using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Media3D;
using TerrainEditor.Utilities;

namespace TerrainEditor.ViewModels
{
    public class TerrainEditorDataContext : PropertyChangeBase
    {
        private DynamicMesh m_selectedTerrain;

        public DynamicMesh SelectedTerrain
        {
            get { return m_selectedTerrain; }
            set
            {
                if (Equals(value, m_selectedTerrain)) return;
                m_selectedTerrain = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<DynamicMesh> Terrains { get; } = new ObservableCollection<DynamicMesh>();
        public Model3DCollection TerrainsMeshes => new Model3DCollection(Terrains.Select(mesh => mesh.Mesh));

        public TerrainEditorDataContext()
        {
            new PropertyChangeListener(Terrains).PropertyChanged += (sender, args) => OnPropertyChanged(nameof(TerrainsMeshes));

            Terrains.Add(new DynamicMesh(new[]
            {
                new VertexInfo(-5, 5),
                new VertexInfo(5, 5),
                new VertexInfo(5, -4),
                new VertexInfo(-5, -4)
            })
            {
                UvMapping = UvMapping.Mossy,
                FillMode = FillMode.Fill,
                IsClosed = true,
                SplitCornersThreshold = 90,
                SmoothFactor = 5
            });

            SelectedTerrain = Terrains[0];
        }
    }
}