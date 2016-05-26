using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Media3D;

/*
 * TODO
mapping editor & property editor mapping
assets window
multiple terrains

fix mesh generation algorithm
fix focus add new vertex
physics
buttons on the top
status bar
update VertexManipulator when property changed  
*/


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
        public ObservableCollection<DynamicMesh> Terrains { get; }
        public Model3DCollection TerrainsMeshes
        {
            get { return new Model3DCollection(Terrains.Select(mesh => mesh.Mesh)); }
        }

        public TerrainEditorDataContext()
        {
            Terrains = new ObservableCollection<DynamicMesh>();
            Terrains.CollectionChanged += OnTerrainsChanged;

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

        private void OnTerrainsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var newMesh = e.NewItems.Cast<DynamicMesh>().Single();
                    newMesh.PropertyChanged += MeshOnPropertyChanged;
                }
                    break;
                case NotifyCollectionChangedAction.Remove:
                {
                    var newMesh = e.OldItems.Cast<DynamicMesh>().Single();
                    newMesh.PropertyChanged -= MeshOnPropertyChanged;
                }
                    break;
                default:
                    throw new NotImplementedException();
            }
            OnPropertyChanged(nameof(TerrainsMeshes));
        }
        private void MeshOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged(nameof(TerrainsMeshes));
        }
    }
}