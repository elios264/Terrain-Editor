using System;
using System.Windows;
using PropertyTools.Wpf;

namespace TerrainEditor.UserControls.PropertiesEditorControl
{
    public class PropertyControlFactory : DefaultPropertyControlFactory
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

            return base.CreateDefaultControl(property);
        }
    }
}
