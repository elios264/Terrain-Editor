using System.IO;
using System.Windows;
using System.Windows.Media;
using TerrainEditor.Core;
using TerrainEditor.Core.Services;

namespace TerrainEditor.UserControls
{
    internal class File : PropertyChangeBase
    {
        private bool m_isEditing;
        private bool m_isSelected;
        private FileInfo m_info;
        private ImageSource m_preview;

        public string Name
        {
            get { return Info.Name; }
            set
            {
                if (Name == value) return;

                if (!value.EndsWith(Info.Extension) && 
                    ServiceLocator.Get<IDialogBoxService>().ShowNativeDialog("Are you sure you want to change the extension?", "Change extension", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;

                Info.MoveTo(Path.Combine(Info.DirectoryName,value));
                OnPropertyChanged();
            }
        }
        public bool IsEditing
        {
            get { return m_isEditing; }
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
            get { return m_isSelected; }
            set
            {
                if (value == m_isSelected)
                    return;
                m_isSelected = value;
                OnPropertyChanged();
            }
        }
        public FileInfo Info
        {
            get { return m_info; }
            set
            {
                if (Equals(value, m_info))
                    return;
                m_info = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));
            }
        }
        public ImageSource Preview
        {
            get { return m_preview; }
            set
            {
                if (Equals(value, m_preview))
                    return;
                m_preview = value;
                OnPropertyChanged();
            }
        }

    }
}