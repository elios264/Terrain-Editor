using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{

    public partial class SelectMappingDialog : ChildWindow
    {
        public static readonly DependencyProperty SelectedMappingProperty = DependencyProperty.Register(
            "SelectedMapping", typeof (UvMapping), typeof (SelectMappingDialog), new PropertyMetadata(default(UvMapping)));

        public UvMapping SelectedMapping
        {
            get
            {
                return (UvMapping) GetValue(SelectedMappingProperty);
            }
            set
            {
                SetValue(SelectedMappingProperty, value);
            }
        }

        public SelectMappingDialog()
        {
            InitializeComponent();
        }
        private void OnSelectMapping(object sender, MouseButtonEventArgs e)
        {
            SelectedMapping = (UvMapping) Options.SelectedItem;
            Close();
        }
        private void OnEditSourceMapping(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.ShowChildWindowAsync(new UvMappingEditor { Source = (UvMapping) Options.SelectedItem});
        }
    }
}
