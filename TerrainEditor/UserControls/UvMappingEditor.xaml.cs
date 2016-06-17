using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;
using TerrainEditor.ViewModels;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TerrainEditor.UserControls
{
    public partial class UvMappingEditor : ChildWindow
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof (UvMapping), typeof (UvMappingEditor), new PropertyMetadata(default(UvMapping)));

        public UvMapping Source
        {
            get { return (UvMapping)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public UvMappingEditor()
        {
            InitializeComponent();
        }

        private void OnAddBody(object sender, RoutedEventArgs e)
        {
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Rect());
        }
        private void OnSelectEdgeTexture(object sender, RoutedEventArgs e)
        {
            var relativePath = OpenSelectImageDialog(Path.GetDirectoryName(Source.EdgeTexturePath));

            if (relativePath != null)
                Source.EdgeTexturePath = relativePath;
        }
        private void OnSelectFillTexture(object sender, RoutedEventArgs e)
        {
            var relativePath = OpenSelectImageDialog(Path.GetDirectoryName(Source.FillTexturePath));

            if (relativePath != null)
                Source.FillTexturePath = relativePath;
        }

        private static string OpenSelectImageDialog(string previousPath = null)
        {
            previousPath = previousPath != null
                ? Path.GetFullPath(previousPath)
                : null;

            var startPath = ServiceLocator.Get<IResourceProviderService>().WorkPath;
            var dialogService = ServiceLocator.Get<IFileDialogService>();
            var messageService = ServiceLocator.Get<IDialogBoxService>();
            var filter = $"All image files ({string.Join(";", ImageCodecInfo.GetImageEncoders().Select(codec => codec.FilenameExtension).ToArray())})|{string.Join(";", ImageCodecInfo.GetImageEncoders().Select(codec => codec.FilenameExtension).ToArray())}";

            string filename = string.Empty;
            while (true)
            {
                if (!dialogService.ShowOpenFileDialog(ref filename, filter, previousPath ?? startPath))
                    return null;

                if (filename.Contains(startPath))
                    break;

                messageService.ShowNativeDialog("Please select a file inside " + startPath, "Invalid selection");
            }

            var openSelectImageDialog = Utils.GetRelativePath(filename);
            return openSelectImageDialog;
        }
        private void OnRemoveEdgeTexture(object sender, MouseButtonEventArgs e)
        {
            Source.EdgeTexture = null;
        }
        private void OnRemoveFillTexture(object sender, MouseButtonEventArgs e)
        {
            Source.FillTexture = null;
        }
    }
}
