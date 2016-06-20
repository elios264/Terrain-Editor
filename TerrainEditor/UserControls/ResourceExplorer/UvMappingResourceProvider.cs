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
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor.UserControls
{
    internal class UvMappingResourceProvider : IResourceInfoProvider
    {
        private readonly XmlArchive m_xmlArchive = new XmlArchive(typeof(UvMapping));

        public Type ResourceType => typeof(UvMapping);
        public string[] Extensions => new[] {".uvmapping"};

        public Task ShowEditor(object resource, FileInfo info)
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
                    Task.Run(() => Save(resource, info)).ContinueWith(_ => completionSource.SetResult(null));
                    break;
                case MessageBoxResult.No:
                    Reload(resource, info);
                    completionSource.SetResult(null);
                    break;
                case MessageBoxResult.None:
                    completionSource.SetResult(null);
                    break;
                }

                if (args.Cancel == false)
                    uvMappingEditor.Source.RecursivePropertyChanged -= notifier;
            };

            dialogBoxService.ShowCustomDialog(uvMappingEditor);

            return completionSource.Task;
        }
        public void Save(object resource, FileInfo info)
        {
            using (var writeStream = info.Open(FileMode.Create))
                m_xmlArchive.Write(writeStream, (UvMapping)resource);
        }
        public void Reload(object resource, FileInfo info)
        {
            var newObj = (UvMapping)Load(info);
            var oldObject = (UvMapping)resource;

            oldObject.Name = newObj.Name;
            oldObject.Top = newObj.Top;
            oldObject.Left = newObj.Left;
            oldObject.Right = newObj.Right;
            oldObject.Bottom = newObj.Bottom;
            oldObject.EdgeTexture = newObj.EdgeTexture;
            oldObject.FillTexture = newObj.FillTexture;
        }
        public object Load(FileInfo info)
        {
            using (var readStream = info.OpenRead())
                return m_xmlArchive.Read(readStream);
        }
        public ImageSource LoadPreview(FileInfo info)
        {
            try
            {
                using (var readStream = info.OpenRead())
                {
                    var node = XmlArchive.LoadNode(readStream);
                    var previewPath = node.Attributes.First(a => a.Name == nameof(UvMapping.EdgeTexture)).Value;
                    return new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception)
            {
                return new BitmapImage();
            }
        }
    }
}
