using System.IO;
using System.Windows.Media;

namespace TerrainEditor.UserControls
{
    public interface IAssetInfo
    {
        object Asset { get; }
        FileInfo FileInfo { get; }
        ImageSource Preview { get; }

        void ShowEditor();
    }

}