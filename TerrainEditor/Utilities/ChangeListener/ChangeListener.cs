using System;
using System.ComponentModel;

namespace TerrainEditor.Utilities
{
    //credits to https://gist.github.com/thojaw/705450
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
