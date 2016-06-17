using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Media3D;
using TerrainEditor.UserControls;
using TerrainEditor.Utilities;

namespace TerrainEditor.ViewModels
{
    public class TerrainEditorDataContext : PropertyChangeBase
    {
        private Terrain m_selectedTerrain;

        public Terrain SelectedTerrain
        {
            get { return m_selectedTerrain; }
            set
            {
                if (Equals(value, m_selectedTerrain)) return;
                m_selectedTerrain = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Terrain> Terrains { get; } = new ObservableCollection<Terrain>();
        public Model3DCollection TerrainsMeshes => new Model3DCollection(Terrains.Select(mesh => mesh.Mesh));
        public IEnumerable<IResourceInfoProvider> ResourceInfoProviders => new IResourceInfoProvider[] {new UvMappingResourceProvider() };

        public TerrainEditorDataContext()
        {
            new RecursivePropertyChangeListener(Terrains).PropertyChanged += (sender, args) => OnPropertyChanged(nameof(TerrainsMeshes));

            Terrains.Add(new Terrain(new[]
            {
                new VertexInfo(0, 0),
                new VertexInfo(0, 10),
                new VertexInfo(10, 10),
                new VertexInfo(10, 0)
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