using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using PersistDotNet.Persist;
using TerrainEditor.ViewModels;

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
