using System;
using System.Windows.Input;
using System.Windows.Threading;
using MahApps.Metro.Controls;

namespace TerrainEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(() =>
                {
                    HelixViewport3D.Focus();
                    Keyboard.Focus(HelixViewport3D);
                }));
        }

    }
}
