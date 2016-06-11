using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TerrainEditor.UserControls
{
    public interface IResourceInfoProvider
    {
        Type ResourceType { get; }
        bool CanCreateNew { get; }
        string[] Extensions { get; }

        Task ShowEditor(FileInfo info, object resource);
        void SaveToDisk(FileInfo info, object resource);
        object ReloadFromDisk(FileInfo info, object resource);
        ImageSource GetPreview(FileInfo info);
    }
}