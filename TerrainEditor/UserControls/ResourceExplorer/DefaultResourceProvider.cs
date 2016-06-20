using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;

namespace TerrainEditor.UserControls
{
    internal class DefaultResourceProvider : IResourceInfoProvider
    {
        public Type ResourceType => null;
        public string[] Extensions => new string[0];

        public Task ShowEditor(object resource, FileInfo info)
        {
            Process.Start(info.FullName);
            return Task.CompletedTask;
        }
        public void Save(object resource, FileInfo info) {}
        public void Reload(object resource, FileInfo info) {}
        public object Load(FileInfo info) { return null; }
        public ImageSource LoadPreview(FileInfo info)
        {
            using (ShellFile shellFile = ShellFile.FromFilePath(info.FullName))
                return shellFile.Thumbnail.MediumBitmapSource;
        }
    }
}