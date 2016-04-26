using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PakalEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new Action(() =>
                {
                    Viewport3D.Focus();
                    Keyboard.Focus(Viewport3D);
                }));
        }
       
        private void Viewport3DOnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.ChangedButton == MouseButton.Left)
            {
                TerrainEditorViewModel.StartManipulationCommand.ExecuteIfAllowed(mouseButtonEventArgs.GetPosition(Viewport3D));
            }
        }

        private void Viewport3DOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            TerrainEditorViewModel.DeltaManipulationCommand.ExecuteIfAllowed(mouseEventArgs.GetPosition(Viewport3D));
        }

        private void Viewport3DOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.ChangedButton == MouseButton.Left)
            {
                TerrainEditorViewModel.EndManipulationCommand.ExecuteIfAllowed(mouseButtonEventArgs.GetPosition(Viewport3D));
            }
        }

        private void Viewport3DKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift: case Key.RightShift: case Key.LeftCtrl: case Key.RightCtrl:
                    TerrainEditorViewModel.ModifierActivatedCommand.ExecuteIfAllowed(Keyboard.Modifiers);
                    break;
            }
        }

        private void Viewport3DKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift: case Key.RightShift: case Key.LeftCtrl: case Key.RightCtrl:
                    TerrainEditorViewModel.ModifierDeactivatedCommand.ExecuteIfAllowed(Keyboard.Modifiers);
                    break;
            }
        }
    }



}
