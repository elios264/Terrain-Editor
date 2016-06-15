using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    public class Directory : PropertyChangeBase
    {
        private bool m_isExpanded;
        private bool m_isEditing;
        private bool m_isSelected;
        private List<Directory> m_directoriesCache; 

        public string Header
        {
            get
            {
                return DirectoryInfo.Name;
            }
            set
            {
                if (value == DirectoryInfo.Name) return;
                DirectoryInfo.MoveTo(Path.Combine(DirectoryInfo.FullName.Substring(0, DirectoryInfo.FullName.LastIndexOf(DirectoryInfo.Name)),value));
                OnPropertyChanged();
            }
        }
        public IEnumerable<Directory> Directories
        {
            get
            {
                try
                {
                    return m_directoriesCache ?? (m_directoriesCache = DirectoryInfo.EnumerateDirectories().Select(di => new Directory(di) { ParentDirectory = this}).ToList());
                }
                catch (Exception )
                {
                    return Enumerable.Empty<Directory>();
                }
            }
        }
        public IEnumerable<FileInfo> Files
        {
            get { return DirectoryInfo.EnumerateFiles(); }
        }
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
        public bool IsSelected
        {
            get
            {
                return m_isSelected;
            }
            set
            {
                if (value == m_isSelected)
                    return;
                m_isSelected = value;
                OnPropertyChanged();
            }
        }
 
        public Directory ParentDirectory { get; private set; }
        public DirectoryInfo DirectoryInfo { get; }
        public Directory FindDir(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return this;

            var idx = relativePath.IndexOf(Path.DirectorySeparatorChar);

            if (idx != -1)
                return Directories
                    .First(d => d.Header == relativePath.Substring(0, idx))
                    .FindDir(relativePath.Substring(idx + 1));

            return Directories.First(d => d.Header == relativePath);
        }

        public Directory(DirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
        }

        public void Refresh()
        {
            DirectoryInfo.Refresh();

            m_directoriesCache = null;

            OnPropertyChanged(nameof(Directories));
            OnPropertyChanged(nameof(Files));
        }
    }

}