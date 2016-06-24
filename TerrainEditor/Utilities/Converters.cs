using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace TerrainEditor.Utilities
{
    public class SelectManyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var objects = new List<object>();

            foreach (object value in values.Where(value => value != null))
            {
                if (value is IEnumerable)
                    objects.AddRange(( (IEnumerable) value ).Cast<object>().Where(o => o != null));
                else
                    objects.Add(value);
            }

            return objects;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToBooleanConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value == false ? null : Activator.CreateInstance(targetType);
        }
    }

    public class CollectionByIndexProxyConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boxedValueCollection = Activator.CreateInstance(
                typeof(ProxyValueCollection<>).MakeGenericType(value.GetType().GetGenericArguments()[0]),
                value);

            return boxedValueCollection;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public sealed class ProxyValueCollection<T> : ObservableCollection<ProxiedValue<T>>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly ObservableCollection<T> m_realCollection;
        private WeakEvent<NotifyCollectionChangedEventHandler> m_realCollectionCollectionChangedEvent;

        public ProxyValueCollection(ObservableCollection<T> realCollection) 
            : base(Enumerable.Range(0,realCollection.Count).Select(i => new ProxiedValue<T>(i,realCollection)))
        {
            m_realCollection = realCollection;
            m_realCollectionCollectionChangedEvent = new WeakEvent<NotifyCollectionChangedEventHandler>(m_realCollection, nameof(m_realCollection.CollectionChanged));

            m_realCollectionCollectionChangedEvent += OnRealCollectionOnCollectionChanged;
            CollectionChanged += OnBoxedValueCollectionChanged;
        }
        private void OnBoxedValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
            case NotifyCollectionChangedAction.Remove:
                m_realCollectionCollectionChangedEvent -= OnRealCollectionOnCollectionChanged;
                m_realCollection.RemoveAt(args.OldStartingIndex);
                m_realCollectionCollectionChangedEvent += OnRealCollectionOnCollectionChanged;
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            default:
            case NotifyCollectionChangedAction.Add:
                throw new NotImplementedException();
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                throw new ArgumentOutOfRangeException();
            }

        }
        private void OnRealCollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged -= OnBoxedValueCollectionChanged;

            switch (args.Action)
            {
            case NotifyCollectionChangedAction.Add:
                var limit = args.NewStartingIndex + args.NewItems.Count;
                for (int i = args.NewStartingIndex; i < limit; i++)
                    Add(new ProxiedValue<T>(i, m_realCollection));
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveAt(args.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                this[args.OldStartingIndex] = new ProxiedValue<T>(args.OldStartingIndex, m_realCollection);
                break;
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
            default:
                throw new ArgumentOutOfRangeException();
            }

            CollectionChanged += OnBoxedValueCollectionChanged;
        }
    }

    public class ProxiedValue<T>
    {
        private readonly int m_index;
        private readonly ObservableCollection<T> m_list;

        public T Value
        {
            get { return m_list[m_index]; }
            set { m_list[m_index] = value; }
        }
        public ProxiedValue(int index, ObservableCollection<T> list)
        {
            m_index = index;
            m_list = list;
        }
    }

}