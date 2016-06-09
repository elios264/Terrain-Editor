using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        public Task ShowEditor()
        {
            Process.Start(FileInfo.FullName);
            return Task.CompletedTask;
        }
        public void SaveToDisk() {}
        public void ReloadFromDisk() {}
    }
}