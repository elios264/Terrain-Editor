using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Annotations;
using TerrainEditor.Core;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    [UsedImplicitly]
    public class UvAssetInfo : IAssetInfo
    {
        private static readonly XmlArchive Archive = new XmlArchive(typeof(UvMapping));

        private Lazy<UvMapping> m_asset;

        public object Asset => m_asset.Value;
        public ImageSource Preview { get;  }
        public FileInfo FileInfo { get; }

        public UvAssetInfo(FileInfo info)
        {
            FileInfo = info;

            if (info.Exists)
            {
                try
                {
                    var reader = info.OpenRead();
                    var node = XmlArchive.LoadNode(reader);
                    var previewPath = node.Attributes.FirstOrDefault(a => a.Name == "EdgeTexture")?.Value;

                    m_asset = new Lazy<UvMapping>(() => (UvMapping)Archive.Read(node));
                    Preview = previewPath != null
                        ? new BitmapImage(new Uri(previewPath))
                        : new BitmapImage();
                    return;
                }
                catch (Exception) {}
            }

            m_asset = new Lazy<UvMapping>();
            Preview = new BitmapImage();
        }

        public Task ShowEditor()
        {
            var dialogBoxService = ServiceLocator.Get<IDialogBoxService>();
            var uvMappingEditor = new UvMappingEditor { Source = m_asset.Value };

            bool assetChanged = false;
            var notifier = new PropertyChangedEventHandler((sender, args) => assetChanged = assetChanged || !args.PropertyName.Contains(nameof(Segment.Editor)));
            var completionSource = new TaskCompletionSource<object>();

            uvMappingEditor.Source.RecursivePropertyChanged += notifier;
            uvMappingEditor.Closing += (sender, args) =>
            {
                var result = MessageBoxResult.None;

                if (assetChanged)
                    result = dialogBoxService.ShowSimpleDialog("Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel,MessageBoxImage.Question);

                switch (result)
                {
                case MessageBoxResult.Cancel:
                    args.Cancel = true;
                    break;
                case MessageBoxResult.Yes:
                    Task.Run(new Action(SaveToDisk));
                    break;
                case MessageBoxResult.No:
                    Task.Run(new Action(ReloadFromDisk));
                    break;
                }

                if (!args.Cancel)
                {
                    uvMappingEditor.Source.RecursivePropertyChanged -= notifier;
                    completionSource.SetResult(null);
                }
            };

            dialogBoxService.ShowCustomDialog(uvMappingEditor);

            return completionSource.Task;
        }
        public void SaveToDisk()
        {
            var writeStream = FileInfo.Open(FileMode.Create);

            Archive.Write(writeStream, Asset);
            writeStream.Close();
        }
        public void ReloadFromDisk()
        {
            var readStream = FileInfo.OpenRead();
            var mapping = (UvMapping)Archive.Read(readStream);
            readStream.Close();
            m_asset = new Lazy<UvMapping>(() => mapping);
        }
    }
}
