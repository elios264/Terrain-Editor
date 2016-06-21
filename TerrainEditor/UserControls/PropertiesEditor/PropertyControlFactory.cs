using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using PropertyTools.Wpf;

namespace TerrainEditor.UserControls
{
    internal class PropertyControlFactory : DefaultPropertyControlFactory
    {
        protected override FrameworkElement CreateDefaultControl(PropertyItem property)
        {
            var controlAttribute = property.GetAttribute<CustomEditorAttribute>();

            if (controlAttribute != null)
            {
                var control = (FrameworkElement) Activator.CreateInstance(controlAttribute.ControlType);
                control.VerticalAlignment = VerticalAlignment.Center;
                control.SetBinding(FrameworkElement.DataContextProperty, property.CreateBinding());

                return control;
            }


            var c = new TextBoxEx
            {
                AcceptsReturn = property.AcceptsReturn,
                MaxLength = property.MaxLength,
                IsReadOnly = property.IsReadOnly,
                TextWrapping = property.TextWrapping,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalContentAlignment = property.AcceptsReturn ? VerticalAlignment.Top : VerticalAlignment.Center
            };

            if (property.FontFamily != null)
                c.FontFamily = new FontFamily(property.FontFamily);

            if (!double.IsNaN(property.FontSize))
                c.FontSize = property.FontSize;

            if (property.IsReadOnly)
                c.Foreground = Brushes.RoyalBlue;

            var binding = property.CreateBinding(UpdateSourceTrigger.PropertyChanged);
            binding.Delay = 500;

            Type type = property.ActualPropertyType;

            if (property.ActualPropertyType != typeof(string) && (!type.IsValueType || Nullable.GetUnderlyingType(type) != null))
                binding.TargetNullValue = string.Empty;

            c.SetBinding(TextBox.TextProperty, binding);
            return c;
        }
        protected override FrameworkElement CreateSliderControl(PropertyItem property)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add( new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var s = new Slider
            {
                Minimum = property.SliderMinimum,
                Maximum = property.SliderMaximum,
                SmallChange = property.SliderSmallChange,
                LargeChange = property.SliderLargeChange,
                TickFrequency = property.SliderTickFrequency,
                IsSnapToTickEnabled = property.SliderSnapToTicks
            };
            s.SetBinding(RangeBase.ValueProperty, property.CreateBinding());
            g.Children.Add(s);

            var c = new TextBoxEx { IsReadOnly = property.Descriptor.IsReadOnly };

            var formatString = property.FormatString;
            if (formatString != null && !formatString.StartsWith("{"))
                formatString = "{0:" + formatString + "}";

            var binding = property.CreateBinding();
            binding.StringFormat = formatString;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Delay = 500;

            c.SetBinding(TextBox.TextProperty, binding);

            Grid.SetColumn(c, 1);
            g.Children.Add(c);

            return g;
        }
        //protected override FrameworkElement CreateGridControl(PropertyItem property)
        //{
        //    var c = new DataGrid
        //    {
        //        CanDelete = property.ListCanRemove,
        //        CanInsert = property.ListCanAdd,
        //        InputDirection = property.InputDirection,
        //        EasyInsert = property.EasyInsert,
        //        AutoGenerateColumns = property.Columns.Count == 0
        //    };

        //    foreach (var cd in property.Columns)
        //    {
        //        if (cd.PropertyName == string.Empty && property.ListItemItemsSource != null)
        //        {
        //            cd.ItemsSource = property.ListItemItemsSource;
        //        }

        //        c.ColumnDefinitions.Add(cd);
        //    }

        //    c.SetBinding(DataGrid.ItemsSourceProperty, property.CreateBinding());
        //    return c;
        //}
    }
}
