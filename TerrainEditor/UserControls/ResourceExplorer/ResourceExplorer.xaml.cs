using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MoreLinq;
using TerrainEditor.Annotations;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;

namespace TerrainEditor.UserControls
{
    public partial class ResourceExplorer : UserControl , INotifyPropertyChanged, IResourceProviderService
    {
        private static readonly DependencyPropertyKey RootDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(RootDirectory), typeof (Directory), typeof (ResourceExplorer), new PropertyMetadata(default(Directory)));
        private static readonly DependencyPropertyKey SelectedDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(SelectedDirectory), typeof (Directory), typeof (ResourceExplorer), new PropertyMetadata(default(Directory),OnRefreshResources));
        private static readonly DependencyPropertyKey CurrentFilesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CurrentFiles), typeof (IEnumerable<File>), typeof (ResourceExplorer), new PropertyMetadata(Enumerable.Empty<File>()));
        private static readonly DependencyProperty SelectedFileProperty = DependencyProperty.Register(nameof(SelectedFile), typeof (File), typeof (ResourceExplorer), new PropertyMetadata(default(File)));

        public static readonly DependencyProperty ShowAllResourcesProperty = DependencyProperty.Register(nameof(ShowAllResources), typeof(bool), typeof(ResourceExplorer), new PropertyMetadata(true, OnRefreshResources));
        public static readonly DependencyProperty WorkPathProperty = DependencyProperty.Register(nameof(WorkPath), typeof(string), typeof(ResourceExplorer), new FrameworkPropertyMetadata(System.IO.Directory.GetCurrentDirectory(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRootPathChanged));
        public static readonly DependencyProperty ResourceInfoProvidersProperty = DependencyProperty.Register(nameof(ResourceInfoProviders), typeof (IEnumerable<IResourceInfoProvider>), typeof (ResourceExplorer), new PropertyMetadata(Enumerable.Empty<IResourceInfoProvider>(),OnResourceInfoProvidersChanged));

        private bool m_isCutting;
        private DateTime m_lastCleanTime;
        private Point m_startDraggingPoint;
        private readonly DefaultResourceProvider m_defaultResourceProvider;
        private Dictionary<string, IResourceInfoProvider> m_resourceInfoProviders;
        private Dictionary<string, WeakReference> m_resourcesCache;
        private readonly Thread m_previewGenerator;
        private readonly BlockingCollection<File> m_previewGeneratorFileQueue;

        private File SelectedFile
        {
            get { return (File)GetValue(SelectedFileProperty); }
            set { SetValue(SelectedFileProperty, value); }
        }
        private IEnumerable<File> CurrentFiles
        {
            get { return (IEnumerable<File>)GetValue(CurrentFilesPropertyKey.DependencyProperty); }
            set { SetValue(CurrentFilesPropertyKey, value); }
        }
        private Directory RootDirectory
        {
            get { return (Directory)GetValue(RootDirectoryPropertyKey.DependencyProperty); }
            set { SetValue(RootDirectoryPropertyKey, value); }
        }
        private Directory SelectedDirectory
        {
            get { return (Directory)GetValue(SelectedDirectoryPropertyKey.DependencyProperty); }
            set { SetValue(SelectedDirectoryPropertyKey, value); }
        }

        public string WorkPath
        {
            get { return (string)GetValue(WorkPathProperty); }
            set { SetValue(WorkPathProperty, value); }
        }
        public bool ShowAllResources
        {
            get { return (bool)GetValue(ShowAllResourcesProperty); }
            set { SetValue(ShowAllResourcesProperty, value); }
        }
        public IEnumerable<IResourceInfoProvider> ResourceInfoProviders
        {
            get { return (IEnumerable<IResourceInfoProvider>)GetValue(ResourceInfoProvidersProperty); }
            set { SetValue(ResourceInfoProvidersProperty, value); }
        }
        public IEnumerable<IResourceInfoProvider> VisibleResourceInfoProviders
        {
            get { return ResourceInfoProviders.Where(i => i.ResourceType != null); }
        }

        public ResourceExplorer()
        {
            InitializeComponent();
            m_previewGeneratorFileQueue = new BlockingCollection<File>();

            m_previewGenerator = new Thread(() =>
            {
                File nextFile;
                while (m_previewGeneratorFileQueue.TryTake(out nextFile, -1))
                {
                    var preview = ProviderFor(nextFile.Info.Extension).LoadPreview(nextFile.Info);
                    preview.Freeze();
                    nextFile.Preview = preview;
                }
            }) { IsBackground = true, Name = $"{nameof(ResourceExplorer)}_preview_generator_thread"};
            m_previewGenerator.Start();
            
            m_resourcesCache = new Dictionary<string, WeakReference>();
            m_defaultResourceProvider = new DefaultResourceProvider();

            OnResourceInfoProvidersChanged(this,default(DependencyPropertyChangedEventArgs));
            OnRootPathChanged(this,default(DependencyPropertyChangedEventArgs));

            if (!ServiceLocator.IsRegistered<IResourceProviderService>())
                ServiceLocator.Register<IResourceProviderService>(this);
        }
        ~ResourceExplorer()
        {
            m_previewGeneratorFileQueue.CompleteAdding();
            m_previewGeneratorFileQueue.Dispose();
        }

        public object LoadResource(FileInfo info)
        {
            if (!info.FullName.Contains(WorkPath))
                throw new ArgumentException($"the path: {{{info.FullName}}},\n is outside the scope of: {{{WorkPath}}} ");

            if (!info.Exists)
                throw new ArgumentException($"the file: {info.FullName} does not exist");

            lock (m_resourcesCache)
            {
                object resource;
                WeakReference reference;
                string localPath = info.RelativePath();

                if (m_resourcesCache.TryGetValue(localPath, out reference) && (resource = reference.Target) != null)
                    return resource;

                //clean the cache
                var now = DateTime.Now;
                if (now - m_lastCleanTime > TimeSpan.FromMinutes(1))
                {
                    m_resourcesCache = m_resourcesCache.Where(p => p.Value.IsAlive).ToDictionary(p => p.Key, p => p.Value);
                    m_lastCleanTime = now;
                    OnPropertyChanged(nameof(LoadedResources));
                }

                IResourceInfoProvider provider;
                if (m_resourceInfoProviders.TryGetValue(info.Extension, out provider))
                {
                    resource = provider.Load(info);
                    m_resourcesCache[localPath] = new WeakReference(resource);
                    OnPropertyChanged(nameof(LoadedResources));
                    return resource;
                }

                return null;
            }
        }
        public FileInfo InfoForResource(object resource)
        {
            var path = m_resourcesCache.FirstOrDefault(p => p.Value.Target == resource).Key;
            return path == null ? null : new FileInfo(path);
        }
        public IEnumerable<object> LoadedResources
        {
            get { return m_resourcesCache.Values.Select(r => r.Target).Where(o => o != null); }
        }
        public void ShowInExplorer(string relativePath)
        {
            var dirOfResource = RootDirectory.FindDir(Path.GetDirectoryName(relativePath));

            var current = dirOfResource;
            while (current.ParentDirectory != null)
            {
                current = current.ParentDirectory;
                current.IsExpanded = true;
            }

            dirOfResource.IsSelected = true;

            var fileOfResource = CurrentFiles.FirstOrDefault(f => f.Name == Path.GetFileName(relativePath));
            if (fileOfResource != null)
                fileOfResource.IsSelected = true;
        }

        private File FileFor(FileInfo info)
        {
            var fileItem = new File { Info = info, };
            m_previewGeneratorFileQueue.Add(fileItem);
            return fileItem;
        }
        private IResourceInfoProvider ProviderFor(string extension)
        {
            IResourceInfoProvider provider;
            return m_resourceInfoProviders.TryGetValue(extension, out provider)
                ? provider
                : m_defaultResourceProvider;
        }

        private static void OnResourceInfoProvidersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (ResourceExplorer) obj;

            instance.m_resourceInfoProviders = new Dictionary<string, IResourceInfoProvider>();
            foreach (var infoProvider in instance.ResourceInfoProviders)
                foreach (var ext in infoProvider.Extensions)
                {
                    instance.m_resourceInfoProviders.Add(ext, infoProvider);
                }

            instance.OnPropertyChanged(nameof(VisibleResourceInfoProviders));
        }
        private static void OnRootPathChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (ResourceExplorer)obj;

            instance.RootDirectory = new Directory(new DirectoryInfo(instance.WorkPath))
            {
                IsExpanded = true,
                IsSelected = true
            };

            System.IO.Directory.SetCurrentDirectory(Path.GetFullPath(instance.WorkPath));
        }
        private static void OnRefreshResources(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (ResourceExplorer) obj;

            instance.SelectedDirectory.DirectoryInfo.Refresh();

            instance.CurrentFiles = 
                instance.SelectedDirectory.Files
                .Select(instance.FileFor)
                .Where(f => instance.ShowAllResources || instance.m_resourceInfoProviders.ContainsKey(f.Info.Extension))
                .ToList();
        }

        private void TreeViewOnSelectedFolderChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedDirectory = (Directory) e.NewValue;
        }
        private void FileList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(FileList);
        }

        private void OnNewFolder(object sender, RoutedEventArgs e)
        {
            const string newFolderName = "New Folder";

            SelectedDirectory.DirectoryInfo.CreateSubdirectory(newFolderName);
            SelectedDirectory.IsExpanded = true;
            SelectedDirectory.Refresh();

            var newDir = SelectedDirectory.Directories.First(d => d.Header == newFolderName);
            newDir.IsSelected = true;
            newDir.IsEditing = true;
        }
        private void OnRenameFolder(object sender, RoutedEventArgs e)
        {
            SelectedDirectory.IsEditing = true;
        }
        private void OnDeleteFolder(object sender, RoutedEventArgs e)
        {
            if (FileOperationAPIWrapper.Send(SelectedDirectory.DirectoryInfo.FullName))
                SelectedDirectory.ParentDirectory.Refresh();
        }
        private void OnRefreshFolder(object sender, RoutedEventArgs e)
        {
            SelectedDirectory.Refresh();
            OnRefreshResources(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnOpenFolderInExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start(SelectedDirectory.DirectoryInfo.FullName);
        }

        private async void OnEditResource(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedFileInfo = SelectedFile.Info;

            await ProviderFor(selectedFileInfo.Extension).ShowEditor(LoadResource(selectedFileInfo), selectedFileInfo);

            m_previewGeneratorFileQueue.Add(SelectedFile);
            Keyboard.Focus((IInputElement)FileList.ItemContainerGenerator.ContainerFromItem(SelectedFile));
        }
        private void OnCutResource(object sender, ExecutedRoutedEventArgs e)
        {
            OnCopyResource(sender, e);
            m_isCutting = true;
        }
        private void OnCopyResource(object sender, ExecutedRoutedEventArgs e)
        {
            var collection = new StringCollection();

            foreach (var item in FileList.SelectedItems)
                collection.Add(((File)item).Info.FullName);

            Clipboard.SetFileDropList(collection);
            m_isCutting = false;
        }
        private void OnPasteResource(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Clipboard.ContainsFileDropList())
                return;

            var items = Clipboard.GetFileDropList();
            var destination = SelectedDirectory.DirectoryInfo.FullName;

            if (m_isCutting)
            {
                foreach (var sourceItem in items)
                {
                    var destFileName = Path.Combine(destination, Path.GetFileName(sourceItem));
                    var sourcePath = Utils.GetRelativePath(sourceItem);

                    System.IO.File.Move(sourceItem, destFileName);
                    if (m_resourcesCache.ContainsKey(sourcePath))
                        m_resourcesCache.RenameKey(sourcePath, Utils.GetRelativePath(destFileName));
                }
                Clipboard.Clear();
            }
            else
            {
                foreach (var sourceItem in items)
                {
                    var newFileName = Path.GetFileName(sourceItem);
                    var newTarget = Path.Combine(destination, newFileName);

                    int count = 2;
                    while (System.IO.File.Exists(newTarget))
                    {
                        newFileName = $"{Path.GetFileNameWithoutExtension(sourceItem)} Copy {count++}{Path.GetExtension(newFileName)}";
                        newTarget = Path.Combine(destination, newFileName);
                    }

                    System.IO.File.Copy(sourceItem, newTarget);
                }
            }

            OnRefreshResources(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnRenameResource(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedFile.IsEditing = true;

            string prevName = SelectedFile.Info.RelativePath();
            SelectedFile.PropertyChanged += (o, args) =>
            {
                if (args.PropertyName == nameof(SelectedFile.Name))
                {
                    if (m_resourcesCache.ContainsKey(prevName))
                        m_resourcesCache.RenameKey(prevName,((File)o).Info.RelativePath());

                    m_previewGeneratorFileQueue.Add(SelectedFile);
                }
            };

        }
        private void OnRenameTextBoxCreated(object sender, RoutedEventArgs e)
        {
            var txt = (TextBox)sender;
            var last = txt.Text.LastIndexOf('.');
            txt.Select(0,last == -1 ? txt.Text.Length : last);
        }
        private void OnDeleteResource(object sender, ExecutedRoutedEventArgs e)
        {
            if (FileOperationAPIWrapper.Send(string.Join("\0", FileList.SelectedItems.Cast<File>().Select(a => a.Info.FullName))))
                OnRefreshResources(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnNewResource(object sender, ExecutedRoutedEventArgs e)
        {
            var infoProvider = (IResourceInfoProvider)e.Parameter;
            var newFileName = Path.Combine(SelectedDirectory.DirectoryInfo.FullName, infoProvider.ResourceType.Name + infoProvider.Extensions[0]);

            int count = 1;
            while (System.IO.File.Exists(newFileName))
                newFileName = Path.Combine(SelectedDirectory.DirectoryInfo.FullName, infoProvider.ResourceType.Name + count++ + infoProvider.Extensions[0]);

            
            infoProvider.Save(Activator.CreateInstance(infoProvider.ResourceType), new FileInfo(newFileName));
            OnRefreshResources(this, default(DependencyPropertyChangedEventArgs));

            CurrentFiles.ForEach(f => f.IsSelected = false);
            var newFile = CurrentFiles.First(f => f.Info.FullName == newFileName);
            newFile.IsSelected = true;
            newFile.IsEditing = true;
        }
        private void RefreshResources(object sender, ExecutedRoutedEventArgs e)
        {
            OnRefreshResources(this, default(DependencyPropertyChangedEventArgs));
        }
        private void OnPreviewDragResource(object sender, MouseButtonEventArgs e)
        {
            m_startDraggingPoint = e.GetPosition(null);
        }
        private void OnTryDragResource(object sender, MouseEventArgs e)
        {
            var diff = m_startDraggingPoint - e.GetPosition(null);

            if (SelectedFile != null && e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var res = LoadResource(SelectedFile.Info) ?? SelectedFile.Info.RelativePath();
                DragDrop.DoDragDrop(this, res , DragDropEffects.Link);
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}