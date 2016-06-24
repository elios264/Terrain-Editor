using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TerrainEditor.Core.Services;
using TerrainEditor.UserControls;

namespace TerrainEditor.Core
{
    public class EditorWindowManager<TResource> where TResource : class
    {
        private bool m_resourceChanged;
        private readonly FileInfo m_info;
        private readonly TResource m_resource;
        private readonly Window m_editorWindow;
        private readonly IResourceInfoProvider m_provider;
        private readonly TaskCompletionSource<object> m_completionSource;

        public EditorWindowManager(IResourceInfoProvider provider, Window editorWindow, TResource resource, FileInfo info)
        {
            m_info = info;
            m_provider = provider;
            m_resource = resource;
            m_editorWindow = editorWindow;
            m_completionSource = new TaskCompletionSource<object>();
        }
        public Task ShowEditor()
        {
            Subscribe(m_resource, OnResourceChanged);
            m_editorWindow.Closing += OnClosingEditor;

            ServiceLocator.Get<IDialogBoxService>().ShowCustomDialog(m_editorWindow);

            return m_completionSource.Task;
        }

        protected virtual void Subscribe(TResource resource, PropertyChangedEventHandler onChanged)
        {
            var changer = resource as PropertyChangeBase;

            if (changer != null)
                changer.RecursivePropertyChanged += onChanged;
            else
                throw new NotImplementedException($"please override {nameof(Subscribe)} and {nameof(Unsubscribe)} methods since your resource doesn't derive from {nameof(PropertyChangeBase)}");
        }
        protected virtual void Unsubscribe(TResource resource, PropertyChangedEventHandler onChanged)
        {
            (resource as PropertyChangeBase).RecursivePropertyChanged -= onChanged;
        }

        private void OnClosingEditor(object sender, CancelEventArgs args)
        {
            Keyboard.Focus(m_editorWindow);

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
                m_editorWindow.Closing -= OnClosingEditor;
                Unsubscribe(m_resource, OnResourceChanged);
                m_completionSource.SetResult(null);
            }
        }
        private void OnResourceChanged(object sender, PropertyChangedEventArgs args)
        {
            m_resourceChanged = true;
            Unsubscribe(m_resource,OnResourceChanged);
        }
    }
}