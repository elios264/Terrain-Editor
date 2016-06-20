using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TerrainEditor.UserControls
{
    public interface IResourceInfoProvider
    {
        Type ResourceType { get; }
        string[] Extensions { get; }

        Task ShowEditor(object resource, FileInfo info);
        void Save(object resource, FileInfo info);
        void Reload(object resource, FileInfo info);
        object Load(FileInfo info);
        ImageSource LoadPreview(FileInfo info);
    }
}