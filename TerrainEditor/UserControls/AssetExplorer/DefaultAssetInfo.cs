using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace TerrainEditor.UserControls
{
    public class DefaultAssetInfo : IAssetInfo
    {
        public object Asset => null;
        public FileInfo FileInfo { get; }
        public ImageSource Preview => null;

        public DefaultAssetInfo(FileInfo info)
        {
            FileInfo = info;
        }
        public void ShowEditor()
        {
            Process.Start(FileInfo.FullName);
        }
        public void SaveToDisk()
        {
        }
        public void ReloadFromDisk()
        {
        }
    }
}