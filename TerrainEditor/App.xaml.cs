using System.Windows;
using System.Windows.Threading;
using TerrainEditor.Core.Services;

namespace TerrainEditor
{
    public partial class App : Application
    {
        public App()
        {
#if !DEBUG
            DispatcherUnhandledException += OnAppUnhandledException;
#endif
        }
#if !DEBUG
        private static void OnAppUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            ServiceLocator.Get<IDialogBoxService>().ShowNativeDialog("The following error has ocurred: \n" + args.Exception.Message, "Exception");
            args.Handled = true;
        }
#endif
    }
}
