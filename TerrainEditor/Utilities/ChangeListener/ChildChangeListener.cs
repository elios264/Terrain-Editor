using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace TerrainEditor.Utilities
{
    public partial class RecursivePropertyChangeListener : IDisposable
    {
        private class ChildChangeListener : ChangeListener
        {
            private static readonly Type InotifyType = typeof(INotifyPropertyChanged);

            private readonly INotifyPropertyChanged m_value;
            private readonly Type m_type;
            private readonly Dictionary<string, ChangeListener> m_childListeners = new Dictionary<string, ChangeListener>();

            public ChildChangeListener(INotifyPropertyChanged instance, string propertyName = null)
            {
                if (instance == null)
                    throw new ArgumentNullException(nameof(instance));

                m_value = instance;
                m_type = m_value.GetType();

                Subscribe();

                PropertyName = propertyName;
            }
            private void Subscribe()
            {
                m_value.PropertyChanged += value_PropertyChanged;

                var query =
                    from property
                    in m_type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where InotifyType.IsAssignableFrom(property.PropertyType)
                    select property;

                foreach (var property in query)
                {
                    // Declare property as known "Child", then register it
                    m_childListeners.Add(property.Name, null);
                    ResetChildListener(property.Name);
                }
            }


            private void ResetChildListener(string propertyName)
            {
                if (m_childListeners.ContainsKey(propertyName))
                {
                    // Unsubscribe if existing
                    if (m_childListeners[propertyName] != null)
                    {
                        m_childListeners[propertyName].PropertyChanged -= child_PropertyChanged;

                        // Should unsubscribe all events
                        m_childListeners[propertyName].Unsubscribe();
                        m_childListeners[propertyName] = null;
                    }

                    var property = m_type.GetProperty(propertyName);
                    if (property == null)
                        throw new InvalidOperationException($"Was unable to get '{propertyName}' property information from Type '{m_type.Name}'");

                    object newValue = property.GetValue(m_value, null);

                    // Only recreate if there is a new value
                    if (newValue != null)
                    {
                        if (newValue is INotifyCollectionChanged)
                        {
                            m_childListeners[propertyName] =
                                new CollectionChangeListener(newValue as INotifyCollectionChanged, propertyName);
                        }
                        else if (newValue is INotifyPropertyChanged)
                        {
                            m_childListeners[propertyName] =
                                new ChildChangeListener(newValue as INotifyPropertyChanged, propertyName);
                        }

                        if (m_childListeners[propertyName] != null)
                            m_childListeners[propertyName].PropertyChanged += child_PropertyChanged;
                    }
                }
            }
            private void child_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                RaisePropertyChanged(e.PropertyName);
            }
            private void value_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // First, reset child on change, if required...
                ResetChildListener(e.PropertyName);

                // ...then, notify about it
                RaisePropertyChanged(e.PropertyName);
            }

            protected override void RaisePropertyChanged(string propertyName)
            {
                // Special Formatting
                base.RaisePropertyChanged($"{PropertyName}{(PropertyName != null ? "." : null)}{propertyName}");
            }
            public override void Unsubscribe()
            {
                m_value.PropertyChanged -= value_PropertyChanged;

                foreach (var binderKey in m_childListeners.Keys)
                {
                    if (m_childListeners[binderKey] != null)
                        m_childListeners[binderKey].Unsubscribe();
                }

                m_childListeners.Clear();
            }
        }

    }

}
