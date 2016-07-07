using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using TerrainEditor.Core;
using TerrainEditor.Core.Services;
using TerrainEditor.UserControls;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;
using Urho;
using FillMode = TerrainEditor.Viewmodels.Terrains.FillMode;
using Terrain = TerrainEditor.Viewmodels.Terrains.Terrain;

namespace TerrainEditor.Viewmodels
{
    public class TerrainEditorDataContext : PropertyChangeBase
    {
        private Terrain m_selectedTerrain;
        private Node m_terrainNodes;

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
        public IEnumerable<IResourceInfoProvider> ResourceInfoProviders { get;  } = new IResourceInfoProvider[] {new UvMappingResourceProvider() };

        public TerrainEditorDataContext()
        {
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


            Urho.Application.Started += () =>
            {
                m_terrainNodes = ServiceLocator.Get<IUrho3DService>().Scene.CreateChild("TerrainNodes");

                var resource = ServiceLocator.Get<IResourceProviderService>().LoadResource(new FileInfo("Mossy.uvmapping"));
                SelectedTerrain.UvMapping = (UvMapping)resource;

                m_terrainNodes.AddChild(SelectedTerrain.MeshNode);

                new RecursivePropertyChangeListener(Terrains,nameof(Terrains)).PropertyChanged += (sender, args) =>
                {
                    m_terrainNodes.RemoveChildren(true, true, false);
                    m_terrainNodes.AddChild(SelectedTerrain.MeshNode);
                };

            };
            SelectedTerrain = Terrains[0];
        }
    }
}