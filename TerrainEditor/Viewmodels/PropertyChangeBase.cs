using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TerrainEditor.Annotations;
using TerrainEditor.Core;

namespace TerrainEditor.ViewModels
{
    public class PropertyChangeBase : INotifyPropertyChanged
    {
        private readonly Lazy<ChangeListener> m_recursivePropertyChangeListener;

        public event PropertyChangedEventHandler PropertyChanged;
        protected event PropertyChangedEventHandler RecursivePropertyChanged
        {
            add
            {
                m_recursivePropertyChangeListener.Value.PropertyChanged += value;
            }
            remove
            {
                m_recursivePropertyChangeListener.Value.PropertyChanged -= value;
            }
        }

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PropertyChangeBase()
        {
            m_recursivePropertyChangeListener = new Lazy<ChangeListener>(() => ChangeListener.Create(this), true);
        }
    }
}