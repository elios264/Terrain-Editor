using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Core.Services;
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
            return new EditorWindowManager(this, resource, info).ShowEditor();
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

        private class EditorWindowManager
        {
            private bool m_resourceChanged;
            private readonly FileInfo m_info;
            private readonly object m_resource;
            private readonly UvMappingEditor m_uvMappingEditor;
            private readonly UvMappingResourceProvider m_provider;
            private readonly TaskCompletionSource<object> m_completionSource;

            public EditorWindowManager(UvMappingResourceProvider provider, object resource, FileInfo info)
            {
                m_provider = provider;
                m_resource = resource;
                m_info = info;
                m_completionSource = new TaskCompletionSource<object>();
                m_uvMappingEditor = new UvMappingEditor { Source = (UvMapping)resource };
            }
            public Task ShowEditor()
            {
                m_uvMappingEditor.Closing += OnClosingEditor;
                m_uvMappingEditor.Source.RecursivePropertyChanged += OnResourceChanged;

                ServiceLocator.Get<IDialogBoxService>().ShowCustomDialog(m_uvMappingEditor);

                return m_completionSource.Task;
            }

            private void OnClosingEditor(object sender, CancelEventArgs args)
            {
                Keyboard.Focus(m_uvMappingEditor);

                var result = MessageBoxResult.None;

                if (m_resourceChanged)
                    result = ServiceLocator.Get<IDialogBoxService>().ShowNativeDialog("Do you want to save the changes", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                switch (result)
                {
                case MessageBoxResult.Cancel: args.Cancel = true; break;
                case MessageBoxResult.Yes: m_provider.Save(m_resource, m_info); break;
                case MessageBoxResult.No: m_provider.Reload(m_resource, m_info); break;
                }

                if (!args.Cancel)
                {
                    m_uvMappingEditor.Closing -= OnClosingEditor;
                    m_uvMappingEditor.Source.RecursivePropertyChanged -= OnResourceChanged;
                    m_completionSource.SetResult(null);
                }
            }
            private void OnResourceChanged(object sender, PropertyChangedEventArgs args)
            {
                m_resourceChanged = true;
            }
        }

    }
}
