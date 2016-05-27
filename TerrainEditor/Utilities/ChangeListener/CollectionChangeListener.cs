using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace TerrainEditor.Utilities
{
    public partial class PropertyChangeListener : IDisposable
    {
        private class CollectionChangeListener : ChangeListener
        {
            private readonly INotifyCollectionChanged m_value;
            private readonly Dictionary<INotifyPropertyChanged, ChangeListener> m_collectionListeners = new Dictionary<INotifyPropertyChanged, ChangeListener>();
            public CollectionChangeListener(INotifyCollectionChanged collection, string propertyName)
            {
                m_value = collection;
                PropertyName = propertyName;

                Subscribe();
            }
            private void Subscribe()
            {
                m_value.CollectionChanged += value_CollectionChanged;

                foreach (var item in ((IEnumerable)m_value).OfType<INotifyPropertyChanged>())
                {
                    ResetChildListener(item);
                }
            }

            private void ResetChildListener(INotifyPropertyChanged item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                RemoveItem(item);

                ChangeListener listener;

                // Add new
                if (item is INotifyCollectionChanged)
                    listener = new CollectionChangeListener((INotifyCollectionChanged)item, PropertyName);
                else
                    listener = new ChildChangeListener(item);

                listener.PropertyChanged += listener_PropertyChanged;
                m_collectionListeners.Add(item, listener);
            }

            private void RemoveItem(INotifyPropertyChanged item)
            {
                // Remove old
                if (m_collectionListeners.ContainsKey(item))
                {
                    m_collectionListeners[item].PropertyChanged -= listener_PropertyChanged;

                    m_collectionListeners[item].Unsubscribe();
                    m_collectionListeners.Remove(item);
                }
            }


            private void ClearCollection()
            {
                foreach (var key in m_collectionListeners.Keys)
                {
                    m_collectionListeners[key].Unsubscribe();
                }

                m_collectionListeners.Clear();
            }
            private void value_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                RaisePropertyChanged(PropertyName);

                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearCollection();
                }
                else
                {
                    // Don't care about e.Action, if there are old items, Remove them...
                    if (e.OldItems != null)
                    {
                        foreach (INotifyPropertyChanged item in e.OldItems.OfType<INotifyPropertyChanged>())
                            RemoveItem(item);
                    }

                    // ...add new items as well
                    if (e.NewItems != null)
                    {
                        foreach (INotifyPropertyChanged item in e.NewItems.OfType<INotifyPropertyChanged>())
                            ResetChildListener(item);
                    }
                }
            }
            private void listener_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // ...then, notify about it
                RaisePropertyChanged($"{PropertyName}{(PropertyName != null ? "[]." : null)}{e.PropertyName}");
            }
            public override void Unsubscribe()
            {
                ClearCollection();
                m_value.CollectionChanged -= value_CollectionChanged;
            }
        }
    }
}
