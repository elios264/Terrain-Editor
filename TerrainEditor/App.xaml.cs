using System;
using System.Windows;
using System.Windows.Threading;
using TerrainEditor.Core;
using TerrainEditor.Core.Services;

namespace TerrainEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += OnAppUnhandledException;
        }
        private static void OnAppUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            ServiceLocator.Get<IDialogBoxService>().ShowNativeDialog("The following error has ocurred: \n" + args.Exception.Message, "Exception");
            args.Handled = true;
        }

    }
}
