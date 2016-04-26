using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace PakalEditor
{
    public static class HelixViewport3DAssistant
    {
        private static readonly Dictionary<object,HelixViewport3D> Current = new Dictionary<object, HelixViewport3D>();

        public static readonly DependencyProperty Viewport3DChildrenProperty = DependencyProperty.RegisterAttached(
            "Viewport3DChildren", typeof (ObservableCollection<ModelVisual3D>), typeof (HelixViewport3DAssistant), new PropertyMetadata(null,OnBoundChildrenChanged));

        private static void OnBoundChildrenChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var helixViewport3D = (HelixViewport3D) dependencyObject;

            var newValue =  (ObservableCollection<ModelVisual3D>) e.NewValue;
            var oldValue =  (ObservableCollection<ModelVisual3D>) e.OldValue;

            if (oldValue != null)
            {
                Current.Remove(oldValue);
                oldValue.CollectionChanged -= OnCollectionChanged;
            }
            if (newValue != null)
            {
                Current.Add(newValue,helixViewport3D);
                newValue.CollectionChanged += OnCollectionChanged;
            }
            

            helixViewport3D.Children.Clear();

            foreach (ModelVisual3D visual3D in newValue ?? Enumerable.Empty<ModelVisual3D>())
            {
                helixViewport3D.Children.Add(visual3D);
            }

        }

        private static void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            var helixViewport3D = Current[sender];

            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    helixViewport3D.Children.Add(notifyCollectionChangedEventArgs.NewItems.Cast<ModelVisual3D>().Single());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    helixViewport3D.Children.Remove(notifyCollectionChangedEventArgs.OldItems.Cast<ModelVisual3D>().Single());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void SetViewport3DChildren(DependencyObject element, ObservableCollection<ModelVisual3D> value)
        {
            element.SetValue(Viewport3DChildrenProperty, value);
        }

        public static ObservableCollection<ModelVisual3D> GetViewport3DChildren(DependencyObject element)
        {
            return (ObservableCollection<ModelVisual3D>) element.GetValue(Viewport3DChildrenProperty);
        }

    }
}