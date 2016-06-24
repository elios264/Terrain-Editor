using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Core;
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor.UserControls
{
    internal class UvMappingResourceProvider : IResourceInfoProvider
    {
        private readonly YamlArchive m_yamlArchive = new YamlArchive(typeof(UvMapping));

        public Type ResourceType => typeof(UvMapping);
        public string[] Extensions => new[] {".uvmapping"};

        public Task ShowEditor(object resource, FileInfo info)
        {
            var editor = new UvMappingControls.UvMappingEditor { Source = (UvMapping)resource };

            return new EditorWindowManager<UvMappingControls.UvMappingEditor,UvMapping>(this, editor, editor.Source, info).ShowEditor();
        }
        public void Save(object resource, FileInfo info)
        {
            using (var writeStream = info.Open(FileMode.Create))
                m_yamlArchive.Write(writeStream, (UvMapping)resource);
        }
        public void Reload(object resource, FileInfo info)
        {
            var newObj = (UvMapping)Load(info);
            var oldObject = (UvMapping)resource;

            oldObject.Name = newObj.Name;
            oldObject.Top = newObj.Top;
            oldObject.Left = newObj.Left;
            oldObject.Right = newObj.Right;
            oldObject.Bottom = newObj.Bottom;
            oldObject.EdgeTexture = newObj.EdgeTexture;
            oldObject.FillTexture = newObj.FillTexture;
        }
        public object Load(FileInfo info)
        {
            using (var readStream = info.OpenRead())
                return m_yamlArchive.Read(readStream);
        }
        public ImageSource LoadPreview(FileInfo info)
        {
            try
            {
                using (var readStream = info.OpenRead())
                {
                    var node = YamlArchive.LoadNode(readStream);
                    var previewPath = node.Attributes.First(a => a.Name == nameof(UvMapping.EdgeTexture)).Value;
                    return new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception)
            {
                return new BitmapImage();
            }
        }
    }

}
