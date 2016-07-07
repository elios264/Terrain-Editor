using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using TerrainEditor.Core.Services;
using Urho;
using UrhoController = Urho.Application;
using Panel = System.Windows.Forms.Panel;
using UserControl = System.Windows.Controls.UserControl;

namespace TerrainEditor.UserControls.Urho3D
{
    public partial class UrhoViewport3D : UserControl , IDisposable
    {
        public static readonly DependencyProperty ControllerTypeProperty = DependencyProperty.Register(nameof(ControllerType), typeof(Type), typeof(UrhoViewport3D), new PropertyMetadata(default(Type),OnControllerChanged));

        public Type ControllerType
        {
            get { return (Type) GetValue(ControllerTypeProperty); }
            set { SetValue(ControllerTypeProperty, value); }
        }
        public UrhoController Controller { get; private set; }

        public UrhoViewport3D()
        {
            InitializeComponent();
        }

        private static void OnControllerChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var instance = (UrhoViewport3D)obj;

            if (instance.Controller != null)
            {
                instance.Controller.Exit();
                instance.Controller.Dispose();

                ServiceLocator.Unregister<IUrho3DService>();
                instance.Controller = null;
            }

            if (instance.ControllerType != null)
            {
                instance.UrhoHost.Child = new Panel { Dock = DockStyle.Fill };
                var appOpts = new ApplicationOptions(assetsFolder: null)
                {
                    ExternalWindow = instance.UrhoHost.Child.Handle,
                    LimitFps = true,
                    TouchEmulation = false
                };
                instance.Controller = UrhoController.CreateInstance(instance.ControllerType, appOpts);

                instance.Controller.Input.MouseButtonDown += eventArgs =>
                {
                    instance.UrhoHost.Focus();
                    instance.UrhoHost.Child.Focus();
                };

                ServiceLocator.Register((IUrho3DService) instance.Controller);
                Task.Yield().GetAwaiter().OnCompleted(() => instance.Controller.Run());
            }
        }

        public void Dispose()
        {
            if (Controller != null)
            {
                Controller.Exit();
                Controller.Dispose();
                Controller = null;
            }
        }

    }
}
