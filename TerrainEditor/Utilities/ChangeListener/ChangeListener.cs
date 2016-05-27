using System;
using System.ComponentModel;

namespace TerrainEditor.Utilities
{
    public partial class PropertyChangeListener : IDisposable
    {
        private abstract class ChangeListener : INotifyPropertyChanged
        {
            public PropertyChangedEventHandler ChangedEventHandler;
            protected string PropertyName;

            public event PropertyChangedEventHandler PropertyChanged
            {
                add
                {
                    ChangedEventHandler += value;
                }
                remove
                {
                    ChangedEventHandler -= value;
                }
            }

            protected virtual void RaisePropertyChanged(string propertyName)
            {
                ChangedEventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public abstract void Unsubscribe();


        }
    }
}
