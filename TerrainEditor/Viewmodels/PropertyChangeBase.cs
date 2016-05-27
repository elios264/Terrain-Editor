using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TerrainEditor.Annotations;
using TerrainEditor.Utilities;

namespace TerrainEditor.ViewModels
{
    public class PropertyChangeBase : INotifyPropertyChanged
    {
        private readonly PropertyChangeListener m_recursivePropertyChangeListener;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler RecursivePropertyChanged
        {
            add
            {
                m_recursivePropertyChangeListener.PropertyChanged += value;
            }
            remove
            {
                m_recursivePropertyChangeListener.PropertyChanged -= value;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PropertyChangeBase()
        {
            m_recursivePropertyChangeListener = new PropertyChangeListener(this);
        }
    }
}