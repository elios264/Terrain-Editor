using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Media3D;

namespace TerrainEditor.ViewModels
{
    public class TerrainEditorDataContext : ViewModelBase
    {
        public ObservableCollection<DynamicMesh> Terrains { get; }
        public Model3DCollection TerrainsMeshes
        {
            get { return new Model3DCollection(Terrains.Select(mesh => mesh.Mesh)); }
        }

        public TerrainEditorDataContext()
        {
            Terrains = new ObservableCollection<DynamicMesh>();
            Terrains.CollectionChanged += OnTerrainsChanged;

            Terrains.Add(new DynamicMesh
            {
                Vertices = new ObservableCollection<VertexInfo>
                {
                    new VertexInfo(-5, 5),
                    new VertexInfo(5, 5),
                    new VertexInfo(5, -4),
                    new VertexInfo(-5, -4)
                },
                UvMapping = UvMapping.Pipe,FillMode = FillMode.None,IsClosed = false
            });
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