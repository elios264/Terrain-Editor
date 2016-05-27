using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TerrainEditor.Utilities
{
    public partial class PropertyChangeListener : IDisposable
    {
        private readonly INotifyPropertyChanged m_value;
        private readonly string m_name;
        private ChangeListener m_changeListener;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (m_changeListener == null)
                {
                    m_changeListener = m_value is INotifyCollectionChanged
                        ? new CollectionChangeListener((INotifyCollectionChanged)m_value,m_name)
                        : (ChangeListener)new ChildChangeListener(m_value,m_name);
                }

                m_changeListener.PropertyChanged += value;
            }
            remove
            {
                m_changeListener.PropertyChanged -= value;

                if (m_changeListener.ChangedEventHandler == null)
                {
                    m_changeListener.Unsubscribe();
                    m_changeListener = null;
                }
            }
        }

        public PropertyChangeListener(INotifyPropertyChanged value, string name = null)
        {
            m_value = value;
            m_name = name;
        }
        public void Dispose()
        {
            m_changeListener.ChangedEventHandler = null;
            m_changeListener.Unsubscribe();
            m_changeListener = null;
        }
    }
}