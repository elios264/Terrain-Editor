using System.Windows;
using System.Windows.Controls;
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
        }

        private void OnAddBody(object sender, RoutedEventArgs e)
        {
            ((Segment)((Button)sender).CommandParameter).Bodies.Add(new Rect());
        }

    }
}
