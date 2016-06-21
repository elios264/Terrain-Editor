using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Input;
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor.UserControls
{
    public partial class UvMappingEditor : ChildWindow
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof (UvMapping), typeof (UvMappingEditor), new PropertyMetadata(default(UvMapping)));
        private static readonly DependencyProperty BodyCountProperty = DependencyProperty.Register( nameof(BodyCount), typeof(int), typeof(UvMappingEditor), new PropertyMetadata(3));
        private static readonly DependencyProperty CapsTooProperty = DependencyProperty.Register( nameof(CapsToo), typeof(bool), typeof(UvMappingEditor), new PropertyMetadata(false));

        private int BodyCount
        {
            get { return (int)GetValue(BodyCountProperty); }
            set { SetValue(BodyCountProperty, value); }
        }
        private bool CapsToo
        {
            get { return (bool)GetValue(CapsTooProperty); }
            set { SetValue(CapsTooProperty, value); }
        }

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
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Point());
        }
        private void OnRegenerateBodies(object sender, RoutedEventArgs e)
        {
            var seg = ((Segment)((Button)sender).CommandParameter);

            var startPoint = seg.Bodies.Count > 0
                ? seg.Bodies[0]
                : new Point();

            if (CapsToo)
            {
                seg.LeftCap = new Point(startPoint.X - seg.CapSize.Width,startPoint.Y);
                seg.RightCap = new Point(startPoint.X + BodyCount*seg.BodySize.Width,startPoint.Y);
            }

            seg.Bodies = new ObservableCollection<Point>(Enumerable.Range(0,BodyCount).Select(i =>
            {
                var p = new Point(startPoint.X,startPoint.Y);
                startPoint.X += seg.BodySize.Width;
                return p;
            }));

        }
        private void OnChangeBodyCount(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && BodyCount < 100)
                BodyCount++;
            else if (e.ChangedButton == MouseButton.Right && BodyCount > 0)
                BodyCount--;
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
        private void OnRemoveEdgeTexture(object sender, RoutedEventArgs routedEventArgs)
        {
            Source.EdgeTexture = null;
        }
        private void OnRemoveFillTexture(object sender, RoutedEventArgs routedEventArgs)
        {
            Source.FillTexture = null;
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
    }

    
}
