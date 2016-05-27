using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TerrainEditor.Utilities
{
    public abstract class ChangeListener : INotifyPropertyChanged, IDisposable
    {
        private PropertyChangedEventHandler m_changedEventHandler;

        protected string PropertyName;
        protected abstract void Unsubscribe();
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                m_changedEventHandler += value;
            }
            remove
            {
                m_changedEventHandler -= value;
            }
        }
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            m_changedEventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            m_changedEventHandler = null;
            Unsubscribe();
            GC.SuppressFinalize(this);
        }

        ~ChangeListener()
        {
            Unsubscribe();
        }

        public static ChangeListener Create(INotifyPropertyChanged value, string propertyName = null)
        {
            return value is INotifyCollectionChanged 
                ? new CollectionChangeListener((INotifyCollectionChanged) value, propertyName) 
                : (ChangeListener) new ChildChangeListener(value, propertyName);
        }
    }
}
