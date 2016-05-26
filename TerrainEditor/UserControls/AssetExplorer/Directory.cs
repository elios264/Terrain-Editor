using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    public class Directory : PropertyChangeBase
    {
        private readonly DirectoryInfo m_info;
        private bool m_isExpanded;
        private bool m_isEditing;
        private bool m_isSelected;
        private List<Directory> m_directoriesCache; 
        private List<FileInfo> m_filesCache;

        public string Header
        {
            get
            {
                return m_info.Name;
            }
            set
            {
                if (value == m_info.Name) return;
                m_info.MoveTo(Path.Combine(Path.GetDirectoryName(m_info.FullName), value)); 
            }
        }
        public IEnumerable<Directory> Directories
        {
            get
            {
                try
                {
                    return m_directoriesCache ?? (m_directoriesCache = m_info.EnumerateDirectories().Select(di => new Directory(di) { ParentDirectory = this}).ToList());
                }
                catch (Exception )
                {
                    return Enumerable.Empty<Directory>();
                }
            }
        }
        public IEnumerable<FileInfo> Files
        {
            get
            {
                try
                {
                    return m_filesCache ?? (m_filesCache = m_info.EnumerateFiles().ToList());
                }
                catch (Exception)
                {
                    return Enumerable.Empty<FileInfo>();
                }
            }
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
        public bool Exists
        {
            get
            {
                return m_info.Exists;
            }
        } 
        public Directory ParentDirectory { get; private set; }

        public Directory(DirectoryInfo directoryInfo)
        {
            m_info = directoryInfo;
        }
        public DirectoryInfo GetDirectoryInfo()
        {
            return m_info;
        }
        public void Refresh()
        {
            m_info.Refresh();

            m_directoriesCache = null;
            m_filesCache = null;

            OnPropertyChanged(nameof(Exists));
            OnPropertyChanged(nameof(Directories));
            OnPropertyChanged(nameof(Files));
        }
    }
}