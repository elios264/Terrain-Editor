using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{
    public partial class UvMappingEditor : ChildWindow
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof (UvMapping), typeof (UvMappingEditor), new PropertyMetadata(default(UvMapping)));

        public UvMapping Source
        {
            get
            {
                return (UvMapping) GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty, value);
            }
        }

        public UvMappingEditor()
        {
            InitializeComponent();
            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            Keyboard.Focus(this);
        }
        private void OnAddBody(object sender, RoutedEventArgs e)
        {
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Rect());
        }

    }
}
