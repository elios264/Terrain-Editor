using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.Annotations;
using TerrainEditor.Core.Services;
using TerrainEditor.Utilities;
using TerrainEditor.Viewmodels.Terrains;

namespace TerrainEditor.UserControls.UvMappingControls
{

    public partial class SelectMappingDialog : ChildWindow , INotifyPropertyChanged
    {
        private static IResourceProviderService ResourceProvider { get;  } = ServiceLocator.Get<IResourceProviderService>();
        public IEnumerable<UvMapping> CachedMappings => ResourceProvider.LoadedResources.OfType<UvMapping>();

        public static readonly DependencyProperty SelectedMappingProperty = DependencyProperty.Register( nameof(SelectedMapping), typeof (UvMapping), typeof (SelectMappingDialog), new PropertyMetadata(default(UvMapping)));

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

            new WeakEvent<PropertyChangedEventHandler>(ResourceProvider, "PropertyChanged").add((sender, args) =>
            {
                if (args.PropertyName == nameof(ResourceProvider.LoadedResources))
                    OnPropertyChanged(nameof(CachedMappings));
            });
        }
        private void OnSelectMapping(object sender, MouseButtonEventArgs e)
        {
            SelectedMapping = (UvMapping) Options.SelectedItem;
            Close();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
