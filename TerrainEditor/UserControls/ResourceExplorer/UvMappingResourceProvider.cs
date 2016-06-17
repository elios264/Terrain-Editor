using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Core.Services;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    internal class UvMappingResourceProvider : IResourceInfoProvider
    {
        private readonly XmlArchive m_xmlArchive = new XmlArchive(typeof(UvMapping));
        public Type ResourceType => typeof(UvMapping);
        public bool CanCreateNew => true;
        public string[] Extensions => new[] {".uvmapping"};

        public Task ShowEditor(FileInfo info, object resource)
        {
            var dialogBoxService = ServiceLocator.Get<IDialogBoxService>();
            var uvMappingEditor = new UvMappingEditor { Source = (UvMapping)resource };

            bool assetChanged = false;
            var notifier = new PropertyChangedEventHandler((sender, args) => assetChanged = assetChanged || !args.PropertyName.Contains(nameof(Segment.Editor)));
            var completionSource = new TaskCompletionSource<object>();

            uvMappingEditor.Source.RecursivePropertyChanged += notifier;
            uvMappingEditor.Closing += (sender, args) =>
            {
                Keyboard.Focus(uvMappingEditor);
                var result = MessageBoxResult.None;

                if (assetChanged)
                    result = dialogBoxService.ShowNativeDialog("Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                switch (result)
                {
                case MessageBoxResult.Cancel:
                    args.Cancel = true;
                    break;
                case MessageBoxResult.Yes:
                    Task.Run(() => SaveToDisk(info, resource)).ContinueWith(_ => completionSource.SetResult(null));
                    break;
                case MessageBoxResult.No:
                    ReloadFromDisk(info, resource);
                    completionSource.SetResult(null);
                    break;
                case MessageBoxResult.None:
                    completionSource.SetResult(null);
                    break;
                }

                if (!args.Cancel)
                    uvMappingEditor.Source.RecursivePropertyChanged -= notifier;
            };

            dialogBoxService.ShowCustomDialog(uvMappingEditor);

            return completionSource.Task;
        }
        public void SaveToDisk(FileInfo info, object resource)
        {
            resource = resource ?? new UvMapping();

            using (var writeStream = info.Open(FileMode.Create))
                m_xmlArchive.Write(writeStream, (UvMapping)resource);
        }
        public object ReloadFromDisk(FileInfo info, object resource)
        {
            using (var readStream = info.OpenRead())
            {
                var reloadedObject = (UvMapping)m_xmlArchive.Read(readStream);
                var oldObject = (UvMapping)resource ?? reloadedObject;

                oldObject.Name = reloadedObject.Name;
                oldObject.Top = reloadedObject.Top;
                oldObject.Left = reloadedObject.Left;
                oldObject.Right = reloadedObject.Right;
                oldObject.Bottom = reloadedObject.Bottom;
                oldObject.EdgeTexture = reloadedObject.EdgeTexture;
                oldObject.FillTexture = reloadedObject.FillTexture;

                return oldObject;
            }
        }
        public ImageSource GetPreview(FileInfo info)
        {
            try
            {
                using (var readStream = info.OpenRead())
                {
                    var node = XmlArchive.LoadNode(readStream);
                    var previewPath = node.Attributes.First(a => a.Name == "EdgeTexture").Value;
                    var img = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                    img.Freeze();
                    return img;
                }
            }
            catch (Exception)
            {
                var image = new BitmapImage();
                image.Freeze();
                return image;
            }
        }
    }
}
