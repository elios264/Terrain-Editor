using System.ComponentModel;
using System.Windows;
#if !DEBUG
using System.Windows.Threading;
using TerrainEditor.Core.Services;
#endif
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor
{
    public partial class App : Application
    {
        static App()
        {
            TypeDescriptor.AddAttributes(typeof(Urho.Vector2), new TypeConverterAttribute(typeof(Vector234Converter)));
            TypeDescriptor.AddAttributes(typeof(Urho.Vector3), new TypeConverterAttribute(typeof(Vector234Converter)));
            TypeDescriptor.AddAttributes(typeof(Urho.Vector4), new TypeConverterAttribute(typeof(Vector234Converter)));
        }

#if !DEBUG
        public App()
        {
            DispatcherUnhandledException += OnAppUnhandledException;
        }
        private static void OnAppUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            ServiceLocator.Get<IDialogBoxService>().ShowNativeDialog("The following error has ocurred: \n" + args.Exception.Message, "Exception");
            args.Handled = true;
        }
#endif
    }
}
