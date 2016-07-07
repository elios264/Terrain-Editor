using MahApps.Metro.Controls;

namespace TerrainEditor
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += (_, __) => UrhoViewport3D.Dispose();
        }

    }
}
