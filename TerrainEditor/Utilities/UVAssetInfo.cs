using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.SimpleChildWindow;
using PersistDotNet.Persist;
using TerrainEditor.Annotations;
using TerrainEditor.UserControls;
using TerrainEditor.ViewModels;

namespace TerrainEditor.Utilities
{

    [UsedImplicitly]
    public class UvAssetInfo : IAssetInfo
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(UvMapping));

        private readonly Lazy<UvMapping> m_mappingCache;

        public object Asset => m_mappingCache.Value;
        public ImageSource Preview => m_mappingCache.Value.EdgeTexture;
        public FileInfo FileInfo { get; }


        public UvAssetInfo(FileInfo info)
        {
            FileInfo = info;
            m_mappingCache = new Lazy<UvMapping>(() => (UvMapping) Serializer.Read(FileInfo.OpenRead(), ""),true);
        }

        public void ShowEditor()
        {
            Application.Current.MainWindow.ShowChildWindowAsync(new UvMappingEditor { Source = m_mappingCache.Value });
        }

    }
}
