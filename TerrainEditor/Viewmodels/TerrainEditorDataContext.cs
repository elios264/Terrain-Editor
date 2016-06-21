using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Media3D;
using TerrainEditor.Core;
using TerrainEditor.UserControls;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor.Viewmodels
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
        public IEnumerable<IResourceInfoProvider> ResourceInfoProviders { get;  } = new IResourceInfoProvider[] {new UvMappingResourceProvider() };

        public TerrainEditorDataContext()
        {
            new RecursivePropertyChangeListener(Terrains).PropertyChanged += (sender, args) => OnPropertyChanged(nameof(TerrainsMeshes));

            Terrains.Add(new Terrain(new[]
            {
                new VertexInfo(-5, 5),
                new VertexInfo(5, 5),
                new VertexInfo(5, -5),
                new VertexInfo(-5, -5)
            })
            {
                UvMapping = UvMapping.Mossy,
                FillMode = FillMode.Fill,
                IsClosed = true,
                SmoothFactor = 5
            });

            SelectedTerrain = Terrains[0];
        }
    }
}