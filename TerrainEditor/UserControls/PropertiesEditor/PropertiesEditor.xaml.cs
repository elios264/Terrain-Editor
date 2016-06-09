using System.Windows;
using System.Windows.Controls;

namespace TerrainEditor.UserControls
{
    public partial class PropertiesEditor : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof (object), typeof (PropertiesEditor), new PropertyMetadata(default(object)));

        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }


        public PropertiesEditor()
        {
            InitializeComponent();
        }
    }
}
