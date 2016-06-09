using System.IO;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    public class File : PropertyChangeBase
    {
        private bool m_isEditing;
        private bool m_isSelected;

        public string Name
        {
            get { return AssetInfo.FileInfo.Name; }
            set
            {
                if (Name == value) return;
                AssetInfo.FileInfo.MoveTo(Path.Combine(AssetInfo.FileInfo.DirectoryName,value));
                OnPropertyChanged(nameof(Name));
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

        public IAssetInfo AssetInfo { get; }
        public Directory Parent { get; }

        public File(IAssetInfo info, Directory parent)
        {
            AssetInfo = info;
            Parent = parent;
        }
    }
}