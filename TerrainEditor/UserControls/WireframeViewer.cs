using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using MoreLinq;

namespace TerrainEditor.UserControls
{
    public class WireframeViewer : LinesVisual3D
    {
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
            nameof(Model), typeof(Model3DGroup), typeof(WireframeViewer), new PropertyMetadata(default(Model3DGroup),OnModelChanged));
        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            nameof(IsVisible), typeof(bool), typeof(WireframeViewer), new PropertyMetadata(true,OnVisibilityChanged));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public new Model3DGroup Model
        {
            get { return (Model3DGroup)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        private static void OnVisibilityChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (WireframeViewer)obj;

            if (instance.IsVisible)
                OnModelChanged(obj, dependencyPropertyChangedEventArgs);
            else
                instance.Points = null;
        }

        private static void OnModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (WireframeViewer)obj;

            if (!instance.IsVisible)
            {
                instance.Points = null;
                return;
            }

            instance.Transform = instance.Model.Transform;

            var points = new List<Point3D>();
            foreach (var model in instance.Model.Children.OfType<GeometryModel3D>())
            {
                var geom = (MeshGeometry3D)model.Geometry;

                foreach (var idxs in geom.TriangleIndices.Batch(3))
                {
                    var ids = idxs.ToArray();

                    points.Add(geom.Positions[ids[0]]);
                    points.Add(geom.Positions[ids[1]]);
                    points.Add(geom.Positions[ids[2]]);
                    points.Add(geom.Positions[ids[0]]);
                    points.Add(geom.Positions[ids[2]]);
                    points.Add(geom.Positions[ids[1]]);
                }
            }
            instance.Points = new Point3DCollection(points);
        }

    }
}