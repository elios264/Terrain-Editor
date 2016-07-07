using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;
using Urho;
using MouseButton = System.Windows.Input.MouseButton;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;

namespace TerrainEditor.UserControls.UvMappingControls
{
    public partial class UvMappingEditor : MetroWindow
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof (UvMapping), typeof (UvMappingEditor), new PropertyMetadata(default(UvMapping)));
        private static readonly DependencyProperty BodyCountProperty = DependencyProperty.Register( nameof(BodyCount), typeof(int), typeof(UvMappingEditor), new PropertyMetadata(3));
        private static readonly DependencyProperty CapsTooProperty = DependencyProperty.Register( nameof(CapsToo), typeof(bool), typeof(UvMappingEditor), new PropertyMetadata(false));

        private Point m_panOrigin;
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
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Vector2());
        }
        private void OnRegenerateBodies(object sender, RoutedEventArgs e)
        {
            var seg = ((Segment)((Button)sender).CommandParameter);

            var startPoint = seg.Bodies.Count > 0
                ? seg.Bodies[0]
                : new Vector2();

            if (CapsToo)
            {
                seg.LeftCap = new Vector2(startPoint.X - seg.CapSize.X,startPoint.Y);
                seg.RightCap = new Vector2(startPoint.X + BodyCount*seg.BodySize.X,startPoint.Y);
            }

            seg.Bodies = new ObservableCollection<Vector2>(Enumerable.Range(0,BodyCount).Select(i =>
            {
                var p = new Vector2(startPoint.X,startPoint.Y);
                startPoint.X += seg.BodySize.X;
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
        private void OnZoom(object sender, MouseWheelEventArgs e)
        {
            var scale = e.Delta >= 0 ? 1.1 : 1.0 / 1.1;
            var position = e.GetPosition(DesignArea);
            var matrix = PreviewTransform.Matrix;
            matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);

            if (scale < 1 && matrix.M11 < 1)
            {
                var m = PreviewTransform.Matrix;
                m.OffsetX = m.OffsetY = 0;
                PreviewTransform.Matrix = m;
                return;
            }

            PreviewTransform.Matrix = matrix;
        }
        private void OnStartPan(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                m_panOrigin = e.GetPosition(DesignArea);
                DesignArea.CaptureMouse();
            }
        }
        private void OnPan(object sender, MouseEventArgs e)
        {
            if (!DesignArea.IsMouseCaptured)
                return;

            var position = e.GetPosition(DesignArea) - m_panOrigin;
            var matrix = PreviewTransform.Matrix;

            matrix.TranslatePrepend(position.X, position.Y);
            PreviewTransform.Matrix = matrix;
        }
        private void OnEndPan(object sender, MouseButtonEventArgs e)
        {
            DesignArea.ReleaseMouseCapture();
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
