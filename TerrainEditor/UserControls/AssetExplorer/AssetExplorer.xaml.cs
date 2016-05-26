using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic.FileIO;

namespace TerrainEditor.UserControls
{
    public struct AssetType
    {
        public Type Handler { get; set; }
        public string Extension { get; set; }
    }
    public class AssetTypeCollection : List<AssetType> { }


    public partial class AssetExplorer : UserControl
    {
        private delegate IAssetInfo AssetFactory(FileInfo info);

        private static readonly DependencyPropertyKey RootDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(RootDirectory), typeof (Directory), typeof (AssetExplorer), new PropertyMetadata(default(Directory)));
        private static readonly DependencyPropertyKey SelectedDirectoryPropertyKey = DependencyProperty.RegisterReadOnly(nameof(SelectedDirectory), typeof (Directory), typeof (AssetExplorer), new PropertyMetadata(default(Directory),OnSelectedDirectoryChanged));
        private static readonly DependencyPropertyKey CurrentAssetsPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CurrentAssets), typeof (IEnumerable<IAssetInfo>), typeof (AssetExplorer), new PropertyMetadata(Enumerable.Empty<IAssetInfo>()));

        public static readonly DependencyProperty SelectedAssetProperty = DependencyProperty.Register(nameof(SelectedAsset), typeof (IAssetInfo), typeof (AssetExplorer), new PropertyMetadata(default(IAssetInfo)));
        public static readonly DependencyProperty RootPathProperty = DependencyProperty.Register(nameof(RootPath), typeof (string), typeof (AssetExplorer), new PropertyMetadata(System.IO.Directory.GetCurrentDirectory(), OnRootDirectoryChanged));
        public static readonly DependencyProperty AssetTypesProperty = DependencyProperty.Register(nameof(AssetTypes), typeof (IEnumerable<AssetType>), typeof (AssetExplorer), new PropertyMetadata(Enumerable.Empty<AssetType>(),OnAssetTypesPopulated));
        public static readonly DependencyProperty RootDirectoryProperty = RootDirectoryPropertyKey.DependencyProperty;
        public static readonly DependencyProperty SelectedDirectoryProperty = SelectedDirectoryPropertyKey.DependencyProperty;
        public static readonly DependencyProperty CurrentAssetsProperty = CurrentAssetsPropertyKey.DependencyProperty;

        private Dictionary<string, AssetFactory> m_assetsFactories;
        private readonly Dictionary<string, WeakReference<IAssetInfo>> m_assetsInUse;

        public string RootPath
        {
            get
            {
                return (string) GetValue(RootPathProperty);
            }
            set
            {
                SetValue(RootPathProperty, value);
            }
        }
        public Directory RootDirectory
        {
            get
            {
                return (Directory) GetValue(RootDirectoryProperty);
            }
            private set
            {
                SetValue(RootDirectoryPropertyKey, value);
            }
        }
        public Directory SelectedDirectory
        {
            get
            {
                return (Directory) GetValue(SelectedDirectoryProperty);
            }
            private set
            {
                SetValue(SelectedDirectoryPropertyKey, value);
            }
        }
        public IAssetInfo SelectedAsset
        {
            get
            {
                return (IAssetInfo)GetValue(SelectedAssetProperty);
            }
            set
            {
                SetValue(SelectedAssetProperty, value);
            }
        }
        public IEnumerable<AssetType> AssetTypes
        {
            get
            {
                return (IEnumerable<AssetType>)GetValue(AssetTypesProperty);
            }
            set
            {
                SetValue(AssetTypesProperty, value);
            }
        }
        public IEnumerable<IAssetInfo> CurrentAssets
        {
            get
            {
                return (IEnumerable<IAssetInfo>)GetValue(CurrentAssetsProperty);
            }
            private set
            {
                SetValue(CurrentAssetsPropertyKey, value);
            }
        }

        public AssetExplorer()
        {
            InitializeComponent();
            OnAssetTypesPopulated(this,default(DependencyPropertyChangedEventArgs));
            OnRootDirectoryChanged(this,default(DependencyPropertyChangedEventArgs));

            m_assetsInUse = new Dictionary<string, WeakReference<IAssetInfo>>();
        }

        private static void OnAssetTypesPopulated(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer) obj;
            
            instance.m_assetsFactories = instance.AssetTypes.ToDictionary(
                    at => at.Extension, 
                    at => new AssetFactory(info => (IAssetInfo)Activator.CreateInstance(at.Handler,info)) );
        }
        private static void OnRootDirectoryChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer)obj;

            instance.RootDirectory = new Directory(new DirectoryInfo(instance.RootPath)) { IsExpanded = true, IsSelected = true};
        }
        private static void OnSelectedDirectoryChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer) obj;

            var assets = new List<IAssetInfo>(instance.SelectedDirectory.Files.Count());
            foreach (var file in instance.SelectedDirectory.Files)
            {
                WeakReference<IAssetInfo> usedAsset;
                if (instance.m_assetsInUse.TryGetValue(file.FullName, out usedAsset))
                {
                    IAssetInfo usedAssetInfo;
                    if (usedAsset.TryGetTarget(out usedAssetInfo))
                    {
                        assets.Add(usedAssetInfo);
                        continue;
                    }

                    instance.m_assetsInUse.Remove(file.FullName);
                }

                AssetFactory factory;
                assets.Add(instance.m_assetsFactories.TryGetValue(file.Extension, out factory) 
                    ? factory(file) 
                    : new DefaultAssetInfo(file));
            }

            instance.CurrentAssets = assets;
        }


        private void TreeViewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as TreeViewItem).IsSelected = true;
        }
        private void TreeViewOnSelectedFolderChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedDirectory = (Directory) e.NewValue;
        }
        private void FileList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedAsset?.ShowEditor();
        }

        private void OnNewFolder(object sender, RoutedEventArgs e)
        {
            const string newFolderName = "New Folder";

            SelectedDirectory.GetDirectoryInfo().CreateSubdirectory(newFolderName);
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
            FileSystem.DeleteDirectory(SelectedDirectory.GetDirectoryInfo().FullName, 
                                        UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);

            SelectedDirectory.ParentDirectory.Refresh();
        }
        private void OnRefreshFolder(object sender, RoutedEventArgs e)
        {
            SelectedDirectory.Refresh();
        }

    }
}
