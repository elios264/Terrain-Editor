using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoreLinq;
using TerrainEditor.Annotations;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;

namespace TerrainEditor.UserControls
{
    public partial class AssetExplorer : UserControl , INotifyPropertyChanged, IAssetProviderService
    {
        private static readonly DependencyPropertyKey RootDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(RootDirectory), typeof (Directory), typeof (AssetExplorer), new PropertyMetadata(default(Directory)));
        private static readonly DependencyPropertyKey SelectedDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(SelectedDirectory), typeof (Directory), typeof (AssetExplorer), new PropertyMetadata(default(Directory),OnRefreshFiles));
        private static readonly DependencyPropertyKey CurrentFilesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CurrentFiles), typeof (IEnumerable<File>), typeof (AssetExplorer), new PropertyMetadata(Enumerable.Empty<File>()));

        private static readonly DependencyProperty ShowAllAssetsProperty = DependencyProperty.Register(nameof(ShowAllAssets), typeof(bool), typeof(AssetExplorer), new PropertyMetadata(true, OnRefreshFiles));
        public static readonly DependencyProperty SelectedFileProperty = DependencyProperty.Register(nameof(SelectedFile), typeof (File), typeof (AssetExplorer), new PropertyMetadata(default(File)));
        public static readonly DependencyProperty RootPathProperty = DependencyProperty.Register(nameof(RootPath), typeof (string), typeof (AssetExplorer), new PropertyMetadata(System.IO.Directory.GetCurrentDirectory(), OnRootPathChanged));
        public static readonly DependencyProperty HandlersProperty = DependencyProperty.Register(nameof(Handlers), typeof (IEnumerable<AssetHandlerInfo>), typeof (AssetExplorer), new PropertyMetadata(Enumerable.Empty<AssetHandlerInfo>(),OnAssetTypesPopulated));
        public static readonly DependencyProperty RootDirectoryProperty = RootDirectoryPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectedDirectoryProperty = SelectedDirectoryPropertyKey.DependencyProperty;
        public static readonly DependencyProperty CurrentFilesProperty = CurrentFilesPropertyKey.DependencyProperty;
        
        private Dictionary<string, Type> m_assetInfoMapping;
        private Dictionary<string,WeakReference<IAssetInfo>> m_assetCache;
        private DateTime m_lastCleanTime;
        private bool m_isCutting;

        public string RootPath
        {
            get { return (string)GetValue(RootPathProperty); }
            set { SetValue(RootPathProperty, value); }
        }

        public Directory RootDirectory
        {
            get { return (Directory)GetValue(RootDirectoryProperty); }
            private set { SetValue(RootDirectoryPropertyKey, value); }
        }
        public Directory SelectedDirectory
        {
            get { return (Directory)GetValue(SelectedDirectoryProperty); }
            private set { SetValue(SelectedDirectoryPropertyKey, value); }
        }
        public File SelectedFile
        {
            get { return (File)GetValue(SelectedFileProperty); }
            set { SetValue(SelectedFileProperty, value); }
        }
        public IEnumerable<AssetHandlerInfo> Handlers
        {
            get { return (IEnumerable<AssetHandlerInfo>)GetValue(HandlersProperty); }
            set { SetValue(HandlersProperty, value); }
        }
        public IEnumerable<AssetHandlerInfo> VisibleHandlers
        {
            get { return Handlers.Where(i => i.Name != null); }
        }
        public IEnumerable<File> CurrentFiles
        {
            get { return (IEnumerable<File>)GetValue(CurrentFilesProperty); }
            private set { SetValue(CurrentFilesPropertyKey, value); }
        }
        private bool ShowAllAssets
        {
            get { return (bool)GetValue(ShowAllAssetsProperty); }
            set { SetValue(ShowAllAssetsProperty, value); }
        }

        public AssetExplorer()
        {
            InitializeComponent();

            OnAssetTypesPopulated(this,default(DependencyPropertyChangedEventArgs));
            OnRootPathChanged(this,default(DependencyPropertyChangedEventArgs));

            m_assetCache = new Dictionary<string, WeakReference<IAssetInfo>>();

            if (!ServiceLocator.IsRegistered<IAssetProviderService>())
                ServiceLocator.Register<IAssetProviderService>(this);
        }

        public IAssetInfo Open(FileInfo info)
        {
            if (!info.FullName.Contains(RootPath))
                throw new ArgumentException($"the path: {{{info.FullName}}},\n is outside the scope of: {{{RootPath}}} ");

            if (!info.Exists)
                throw new ArgumentException($"the file: {info.FullName} does not exist");


            WeakReference<IAssetInfo> assetRef;
            IAssetInfo asset;
            if (m_assetCache.TryGetValue(info.FullName, out assetRef) && assetRef.TryGetTarget(out asset))
                return asset;


            lock (m_assetCache)
            {
                //clean the cache
                var now = DateTime.Now;
                if (now - m_lastCleanTime > TimeSpan.FromMinutes(1))
                {
                    m_assetCache = m_assetCache.Where(p =>
                    {
                        IAssetInfo i;
                        return p.Value.TryGetTarget(out i);
                    }).ToDictionary(p => p.Key, p => p.Value);

                    m_lastCleanTime = now;
                }

                Type assetType;
                if (m_assetInfoMapping.TryGetValue(info.Extension, out assetType))
                    m_assetCache[info.FullName] = new WeakReference<IAssetInfo>(asset = (IAssetInfo)Activator.CreateInstance(assetType, info));
                else
                    asset = new DefaultAssetInfo(info);

                return asset;
            }
        }
        public IEnumerable<IAssetInfo> CachedAssets
        {
            get
            {
                return m_assetCache.Values.Select(r =>
                {
                    IAssetInfo info;
                    r.TryGetTarget(out info);
                    return info;
                }).Where(i => i != null);
            }
        }

        private static void OnAssetTypesPopulated(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer) obj;

            instance.m_assetInfoMapping = instance.Handlers.ToDictionary(i => i.Extension, i => i.Handler);
            instance.OnPropertyChanged(nameof(VisibleHandlers));
        }
        private static void OnRootPathChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer)obj;

            instance.RootDirectory = new Directory(new DirectoryInfo(instance.RootPath))
            {
                IsExpanded = true,
                IsSelected = true
            };

            System.IO.Directory.SetCurrentDirectory(Path.GetFullPath(instance.RootPath));
        }
        private static void OnRefreshFiles(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer) obj;

            instance.SelectedDirectory.DirectoryInfo.Refresh();

            instance.CurrentFiles = 
                instance.SelectedDirectory.Files
                .Select(info => new File(instance.Open(info)))
                .Where(f => instance.ShowAllAssets || !(f.AssetInfo is DefaultAssetInfo))
                .Pipe(f => f.AssetInfo.ReloadFromDisk() )
                .ToList();
        }

        private void TreeViewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as TreeViewItem).IsSelected = true;
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
            OnRefreshFiles(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnOpenFolderInExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start(SelectedDirectory.DirectoryInfo.FullName);
        }

        private async void OnEditAsset(object sender, ExecutedRoutedEventArgs e)
        {
            await SelectedFile.AssetInfo.ShowEditor();
            OnRefreshFiles(this, default(DependencyPropertyChangedEventArgs));
            Keyboard.Focus((IInputElement)FileList.ItemContainerGenerator.ContainerFromItem(SelectedFile));
        }
        private void OnCutAssets(object sender, ExecutedRoutedEventArgs e)
        {
            OnCopyAssets(sender, e);
            m_isCutting = true;
        }
        private void OnCopyAssets(object sender, ExecutedRoutedEventArgs e)
        {
            var collection = new StringCollection();

            foreach (var item in FileList.SelectedItems)
                collection.Add(((File)item).AssetInfo.FileInfo.FullName);

            Clipboard.SetFileDropList(collection);
            m_isCutting = false;
        }
        private void OnPasteAssets(object sender, ExecutedRoutedEventArgs e)
        {
            if (!Clipboard.ContainsFileDropList())
                return;

            var items = Clipboard.GetFileDropList();

            var destination = SelectedDirectory.DirectoryInfo.FullName;

            if (m_isCutting)
            {
                foreach (var sourceItem in items)
                    System.IO.File.Move(sourceItem, Path.Combine(destination, Path.GetFileName(sourceItem)));
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

            OnRefreshFiles(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnRenameAsset(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedFile.IsEditing = true;
        }
        private void OnDeleteAssets(object sender, ExecutedRoutedEventArgs e)
        {
            if (FileOperationAPIWrapper.Send(string.Join("\0", FileList.SelectedItems.Cast<File>().Select(a => a.AssetInfo.FileInfo.FullName))))
                OnRefreshFiles(this,default(DependencyPropertyChangedEventArgs));
        }
        private void OnNewAsset(object sender, ExecutedRoutedEventArgs e)
        {
            var info = (AssetHandlerInfo)e.Parameter;
            var newFileName = Path.Combine(SelectedDirectory.DirectoryInfo.FullName, info.Name + info.Extension);

            int count = 1;
            while (System.IO.File.Exists(newFileName))
                newFileName = Path.Combine(SelectedDirectory.DirectoryInfo.FullName, info.Name + count++ + info.Extension);


            var handler = (IAssetInfo)Activator.CreateInstance(info.Handler, new FileInfo(newFileName));
            handler.SaveToDisk();

            OnRefreshFiles(this, default(DependencyPropertyChangedEventArgs));

            CurrentFiles.ForEach(f => f.IsSelected = false);
            var newFile = CurrentFiles.First(f => f.AssetInfo.FileInfo.FullName == newFileName);
            newFile.IsSelected = true;
            newFile.IsEditing = true;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public struct AssetHandlerInfo
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public Type Handler { get; set; }
    }
    public class AssetHandlerCollection : List<AssetHandlerInfo> { }
    public interface IAssetProviderService
    {
        string RootPath { get; set; }
        IAssetInfo Open(FileInfo info);
        IEnumerable<IAssetInfo> CachedAssets { get; }
    }

}
