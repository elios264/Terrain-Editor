using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.SimpleChildWindow;
using PersistDotNet.Persist;
using TerrainEditor.Annotations;
using TerrainEditor.UserControls;
using TerrainEditor.Utilities;
using TerrainEditor.ViewModels;

namespace TerrainEditor.Core
{
    [UsedImplicitly]
    public class UvAssetInfo : IAssetInfo
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(UvMapping));

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
                var mapping = (UvMapping)Serializer.Read(readStream, "");
                readStream.Close();
                return mapping;
            }, true);
        }

        public void ShowEditor()
        {
            var observer = ChangeListener.Create(m_mappingCache.Value);
            var assetChanged = false;

            observer.PropertyChanged += (sender, args) => assetChanged = true;

            var uvMappingEditor = new UvMappingEditor { Source = m_mappingCache.Value };
            uvMappingEditor.Closing += (sender, args) =>
            {
                var result = MessageBoxResult.None;

                if (assetChanged)
                    result = MessageBox.Show(Application.Current.MainWindow, "Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel,MessageBoxImage.Question);

                switch (result)
                {
                case MessageBoxResult.Cancel:
                    args.Cancel = true;
                    break;
                case MessageBoxResult.Yes:
                    SaveToDisk();
                    break;
                case MessageBoxResult.No:
                    ReloadFromDisk();
                    break;
                }

                if (!args.Cancel)
                    observer.Dispose();
            };

            Application.Current.MainWindow.ShowChildWindowAsync(uvMappingEditor);
        }
        public void SaveToDisk()
        {
            var writeStream = FileInfo.Open(FileMode.Create);
            Serializer.Write(writeStream, "UvMapping", Asset);
            writeStream.Close();
        }
        public void ReloadFromDisk()
        {
            var readStream = FileInfo.OpenRead();
            var mapping = (UvMapping) Serializer.Read(readStream, "");
            readStream.Close();
            m_mappingCache = new Lazy<UvMapping>(() => mapping);
        }
    }
}
