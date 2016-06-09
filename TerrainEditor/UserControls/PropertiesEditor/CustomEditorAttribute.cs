using System;
using System.Windows;

namespace TerrainEditor.UserControls
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CustomEditorAttribute : Attribute
    {
        public Type ControlType { get; }

        public CustomEditorAttribute(Type controlType)
        {
            if (!typeof(FrameworkElement).IsAssignableFrom(controlType))
                throw new ArgumentException($"{nameof(controlType)} must be derived from {nameof(FrameworkElement)}");

            ControlType = controlType;
        }
    }
}