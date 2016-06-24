using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TerrainEditor.Annotations;
using TerrainEditor.Utilities;

namespace TerrainEditor.Core.Services
{
    public interface IDialogBoxService
    {
        void ShowCustomDialog(Window window, bool asDialog = false);
        MessageBoxResult ShowNativeDialog(string messageBoxText, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
    }

    [IsService(typeof(IDialogBoxService)), UsedImplicitly]
    internal class DialogBoxService : IDialogBoxService
    {
        private static Window ActiveWindow => Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

        public void ShowCustomDialog(Window window, bool asDialog)
        {
            window.Owner = ActiveWindow;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            new WeakEvent<CancelEventHandler>(window.Owner,nameof(Window.Closing)).add((sender, args) =>
            {
                window.Close();
                args.Cancel = window.Owner.OwnedWindows.Cast<Window>().Contains(window);
            });

            if (asDialog)
                window.ShowDialog();
            else
                window.Show();
        }
        public MessageBoxResult ShowNativeDialog(string messageBoxText, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            return MessageBox.Show(ActiveWindow, messageBoxText, caption, button, icon);
        }

        public static readonly DependencyProperty CloseByEscapeProperty = DependencyProperty.RegisterAttached("CloseByEscape", typeof(bool), typeof(DialogBoxService), new FrameworkPropertyMetadata(false, OnEscapeClosesWindowChanged));
        public static void SetCloseByEscape(DependencyObject element, bool value)
        {
            element.SetValue(CloseByEscapeProperty, value);
        }
        public static bool GetCloseByEscape(DependencyObject element)
        {
            return (bool)element.GetValue(CloseByEscapeProperty);
        }
        private static void OnEscapeClosesWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d)) return;

            Window target = (Window)d;

            if (GetCloseByEscape(target))
                target.PreviewKeyDown += Window_PreviewKeyDown;
            else
                target.PreviewKeyDown -= Window_PreviewKeyDown;
        }
        private static void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Window target = (Window)sender;

            if (e.Key == Key.Escape)
                target.Close();
        }
    }
}
