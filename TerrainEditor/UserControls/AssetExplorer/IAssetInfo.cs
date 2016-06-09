using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TerrainEditor.UserControls
{
    public interface IAssetInfo
    {
        object Asset { get; }
        FileInfo FileInfo { get; }
        ImageSource Preview { get; }

        Task ShowEditor();
        void SaveToDisk();
        void ReloadFromDisk();
    }

}