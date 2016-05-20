using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic.FileIO;
using TerrainEditor.Utilities;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    public class Directory : ViewModelBase
    {
        private bool m_isExpanded;
        private bool m_isEditing;

        public Directory Parent { get; private set; }
        public string Header
        {
            get
            {
                return Info.Name;
            }

            set
            {
                if (value == Info.Name) return;
               Info.MoveTo(Path.Combine(Path.GetDirectoryName(Info.FullName), value)); 
            }
        }
        public IEnumerable<Directory> Children
        {
            get
            {
                try
                {
                    return Info
                        .EnumerateDirectories()
                        .Select(di => new Directory(di) { Parent = this});
                }
                catch (Exception )
                {
                    return Enumerable.Empty<Directory>();
                }
            }
        }
        public DirectoryInfo Info { get; }
        public bool IsExpanded
        {
            get
            {
                return m_isExpanded;
            }
            set
            {
                if (value == m_isExpanded)
                    return;
                m_isExpanded = value;
                OnPropertyChanged();
            }
        }
        public bool IsEditing
        {
            get
            {
                return m_isEditing;
            }
            set
            {
                if (value == m_isEditing)
                    return;
                m_isEditing = value;
                OnPropertyChanged();
            }
        }

        public Directory(DirectoryInfo directoryInfo)
        {
            Info = directoryInfo;
        }

        public void Refresh()
        {
            Info.Refresh();
            OnPropertyChanged(nameof(Children));
        }
    }

    public partial class AssetExplorer : UserControl
    {
        public static readonly DependencyProperty RootPathProperty = DependencyProperty.Register(nameof(RootPath), typeof (string), typeof (AssetExplorer), new PropertyMetadata(System.IO.Directory.GetCurrentDirectory(), RootChanged));

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
        
        public AssetExplorer()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                if (FolderList.ItemsSource == null)
                    RootChanged(this,new DependencyPropertyChangedEventArgs());
            };
        }

        private static void RootChanged(DependencyObject obj, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var instance = (AssetExplorer)obj;

            instance.FolderList.ItemsSource = System.IO.Directory.Exists(instance.RootPath) 
                ? new Directory(new DirectoryInfo(instance.RootPath)) { IsExpanded = true}.ToNewArray()
                : Enumerable.Empty<Directory>();
        }
        private void FolderList_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var currentDir = (Directory) e.NewValue;

            try
            {
                FileList.ItemsSource = currentDir.Info.EnumerateFiles();
            }
            catch (Exception )
            {
                FileList.ItemsSource = null;
            }
        }

        private void OnNewFolder(object sender, RoutedEventArgs e)
        {
            var selectedDir = (Directory) ( (FrameworkElement) sender ).DataContext;

            selectedDir.Info.CreateSubdirectory("New Folder");
            selectedDir.IsExpanded = true;
            selectedDir.Refresh();

        }
        private void OnRenameFolder(object sender, RoutedEventArgs e)
        {
            var selectedDir = (Directory)((FrameworkElement)sender).DataContext;

            selectedDir.IsEditing = true;
        }
        private void OnDeleteFolder(object sender, RoutedEventArgs e)
        {
            var selectedDir = (Directory)((FrameworkElement)sender).DataContext;
            FileSystem.DeleteDirectory(selectedDir.Info.FullName,UIOption.AllDialogs,RecycleOption.SendToRecycleBin,UICancelOption.DoNothing);
            selectedDir.Parent?.Refresh();
        }
    }
}
