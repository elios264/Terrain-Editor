using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using elios.Persist;
using TerrainEditor.Annotations;
using TerrainEditor.UserControls;
using TerrainEditor.ViewModels;

namespace TerrainEditor.Core
{
    [UsedImplicitly]
    public class UvAssetInfo : IAssetInfo
    {
        private static readonly XmlArchive Archive = new XmlArchive(typeof(UvMapping));

        private Lazy<UvMapping> m_mappingCache;

        public object Asset => m_mappingCache.Value;
        public ImageSource Preview => m_mappingCache.Value.EdgeTexture;
        public FileInfo FileInfo { get; }

        public UvAssetInfo(FileInfo info)
        {
            FileInfo = info;

            m_mappingCache = new Lazy<UvMapping>(() =>
            {
                var readStream = FileInfo.OpenRead();
                var mapping = (UvMapping)Archive.Read(readStream);
                readStream.Close();
                return mapping;
            }, true);
        }

        public void ShowEditor()
        {
            var uvMappingEditor = new UvMappingEditor { Source = m_mappingCache.Value };

            bool assetChanged = false;
            var notifier = new PropertyChangedEventHandler((sender, args) => assetChanged = assetChanged || !args.PropertyName.Contains(nameof(Segment.Editor)));

            uvMappingEditor.Source.RecursivePropertyChanged += notifier;

            uvMappingEditor.Closing += (sender, args) =>
            {
                var result = MessageBoxResult.None;

                if (assetChanged)
                    result = ServiceLocator.Get<IDialogBoxService>().ShowSimpleDialog("Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel,MessageBoxImage.Question);

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
                    uvMappingEditor.Source.RecursivePropertyChanged -= notifier;
            };

            ServiceLocator.Get<IDialogBoxService>().ShowCustomDialog(uvMappingEditor);
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
            var mapping = (UvMapping) Archive.Read(readStream);
            readStream.Close();
            m_mappingCache = new Lazy<UvMapping>(() => mapping);
        }
    }
}
