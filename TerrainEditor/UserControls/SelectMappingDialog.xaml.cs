﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.SimpleChildWindow;
using TerrainEditor.Core.Services;
using TerrainEditor.ViewModels;

namespace TerrainEditor.UserControls
{

    public partial class SelectMappingDialog : ChildWindow
    {
        public static readonly DependencyProperty SelectedMappingProperty = DependencyProperty.Register(
            nameof(SelectedMapping), typeof (UvMapping), typeof (SelectMappingDialog), new PropertyMetadata(default(UvMapping)));

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
        public IEnumerable<UvMapping> CachedMappings
        {
            get { return ServiceLocator.Get<IResourceProviderService>().LoadedResources.OfType<UvMapping>(); }
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
    }
}
