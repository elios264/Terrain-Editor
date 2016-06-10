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
using TerrainEditor.Core.Services;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    [UsedImplicitly]
    public class UvAssetInfo : IAssetInfo
    {
        private static readonly XmlArchive Archive = new XmlArchive(typeof(UvMapping));

        private Lazy<UvMapping> m_asset;

        public object Asset => m_asset.Value;
        public ImageSource Preview => m_asset.Value.EdgeTexture;
        public FileInfo FileInfo { get; }

        public UvAssetInfo(FileInfo info)
        {
            FileInfo = info;

            if (!info.Exists)
                m_asset = new Lazy<UvMapping>();
            else
                ReloadFromDisk();
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
                    result = dialogBoxService.ShowNativeDialog("Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel,MessageBoxImage.Question);

                switch (result)
                {
                case MessageBoxResult.Cancel:
                    args.Cancel = true;
                    break;
                case MessageBoxResult.Yes:
                    Task.Run(new Action(SaveToDisk)).ContinueWith(_ => completionSource.SetResult(null));
                    break;
                case MessageBoxResult.No:
                    Task.Run(new Action(ReloadFromDisk)).ContinueWith(_ => completionSource.SetResult(null)); ;
                    break;
                }

                if (!args.Cancel)
                    uvMappingEditor.Source.RecursivePropertyChanged -= notifier;
            };

            dialogBoxService.ShowCustomDialog(uvMappingEditor);

            return completionSource.Task;
        }
        public void SaveToDisk()
        {
            using (var writeStream = FileInfo.Open(FileMode.Create))
                Archive.Write(writeStream, Asset);
        }
        public void ReloadFromDisk()
        {
            m_asset = new Lazy<UvMapping>(() =>
            {
                using (var readStream = FileInfo.OpenRead())
                    return (UvMapping)Archive.Read(readStream);
            });
        }
    }
}
