using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TerrainEditor.Utilities
{
    public partial class RecursivePropertyChangeListener : IDisposable
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
                if (m_changeListener != null)
                {
                    m_changeListener.PropertyChanged -= value;

                    if (m_changeListener.ChangedEventHandler == null)
                    {
                        m_changeListener.Unsubscribe();
                        m_changeListener = null;
                    }
                }
            }
        }

        public RecursivePropertyChangeListener(INotifyPropertyChanged value, string name = null)
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