using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    /// <summary>
    /// Interaction logic for UvMappingEditor.xaml
    /// </summary>
    public partial class UvMappingEditor : ChildWindow
    {
        public UvMappingEditor()
        {
            InitializeComponent();
        }

        private void OnAddBody(object sender, RoutedEventArgs e)
        {
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Rect());
        }

    }
}
