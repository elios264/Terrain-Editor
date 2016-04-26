using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using PakalEditor.mvvm_stuff;

namespace PakalEditor
{

    class TerrainEditorViewModel : ViewModelBase
    {
        private Terrain2DVisual m_current_terrain;
        private ObservableCollection<ModelVisual3D> m_models;


        public Command<Viewport3D> NewTerrainCommand { get; }

        public Command<Point> StartManipulationCommand { get; }
        public Command<Point> DeltaManipulationCommand { get; }
        public Command<Point> EndManipulationCommand { get; }

        public Command<ModifierKeys> ModifierActivatedCommand { get; }
        public Command<ModifierKeys> ModifierDeactivatedCommand { get; }

        public ObservableCollection<ModelVisual3D> Models
        {
            get { return m_models; }
            set
            {
                if (Equals(value, m_models)) return;
                m_models = value;
                OnPropertyChanged(() => Models);
            }
        }
        public Terrain2DVisual CurrentTerrain
        {
            get { return m_current_terrain; }
            set
            {
                if (Equals(value, m_current_terrain)) return;
                m_current_terrain = value;
                OnPropertyChanged(() => CurrentTerrain);
            }
        }



        //terriain properties
        [EditorProperty]
        public uint PixelsPerUnit
        {
            get
            {
                return CurrentTerrain?.PixelsPerUnit ?? 0;
            }
            set
            {
                if (CurrentTerrain.PixelsPerUnit == value) return;
                CurrentTerrain.PixelsPerUnit = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }

        [EditorProperty]
        public bool Closed
        {
            get
            {
                return CurrentTerrain?.Closed ?? false;
            }
            set
            {
                if (CurrentTerrain.Closed == value) return;
                CurrentTerrain.Closed = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public bool ShowWireFrame
        {
            get { return CurrentTerrain?.ShowWireFrame ?? false; }
            set
            {
                if (CurrentTerrain.ShowWireFrame == value) return;
                CurrentTerrain.ShowWireFrame = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public int SmoothFactor
        {
            get { return CurrentTerrain?.SmoothFactor ?? 0; }
            set
            {
                if (CurrentTerrain.SmoothFactor == value) return;
                CurrentTerrain.SmoothFactor = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public Color AmbientColor
        {
            get { return CurrentTerrain.AmbientColor; }
            set
            {
                if (CurrentTerrain.AmbientColor == value) return;
                CurrentTerrain.AmbientColor = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public double StrechThreshold
        {
            get { return CurrentTerrain?.StrechThreshold ?? 0; }
            set
            {
                if (CurrentTerrain.StrechThreshold == value) return;
                CurrentTerrain.StrechThreshold = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public int SplitCornersThreshold
        {
            get { return CurrentTerrain?.SplitCornersThreshold?? 0; }
            set
            {
                if (CurrentTerrain.SplitCornersThreshold == value) return;
                CurrentTerrain.SplitCornersThreshold = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public bool SplitWhenDifferent
        {
            get { return CurrentTerrain?.SplitWhenDifferent ?? false; }
            set
            {
                if (CurrentTerrain.SplitWhenDifferent == value) return;
                CurrentTerrain.SplitWhenDifferent = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }
        [EditorProperty]
        public Terrain2DVisual.FillMode Mode
        {
            get { return CurrentTerrain.Mode; }
            set
            {
                if (CurrentTerrain.Mode == value) return;
                CurrentTerrain.Mode = value;
                CurrentTerrain.Tesellate();
                OnPropertyChanged();
            }
        }


        public TerrainEditorViewModel()
        {
            Models = new ObservableCollection<ModelVisual3D>
            {
                new SunLight { Ambient = 1.0},
                new GridLinesVisual3D
                {
                    Fill = new SolidColorBrush(Color.FromRgb(93, 93, 93)),
                    Thickness = 0.05,
                },
            };

            NewTerrainCommand = new Command<Viewport3D>(CreateTerrain);
            StartManipulationCommand = new Command<Point>(StartManipulation, point => CurrentTerrain != null );
            DeltaManipulationCommand = new Command<Point>(DeltaManipulation, point => CurrentTerrain != null );
            EndManipulationCommand = new Command<Point>(EndManipulation, point => CurrentTerrain != null );

            ModifierActivatedCommand = new Command<ModifierKeys>(ModifierActivated, m => CurrentTerrain != null );
            ModifierDeactivatedCommand = new Command<ModifierKeys>(ModifierDeactivated, m => CurrentTerrain != null );
        }

        private void CreateTerrain(Viewport3D viewport)
        {
            if (CurrentTerrain != null)
            {
                CurrentTerrain.EndManipulation(new Point());
                CurrentTerrain.ModificerDeactivated(ModifierKeys.None);
            }

            CurrentTerrain = new Terrain2DVisual(viewport);
            Models.Add(CurrentTerrain);
            OnPropertyChanged("");
        }

        private void StartManipulation(Point point)
        {
            CurrentTerrain.StartManipulation(point);
        }

        private void DeltaManipulation(Point point)
        {
            CurrentTerrain.DeltaManipulation(point);
        }

        private void EndManipulation(Point point)
        {
            CurrentTerrain.EndManipulation(point);
        }

        private void ModifierActivated(ModifierKeys keys)
        {
            CurrentTerrain.ModifierActivated(keys);
        }

        private void ModifierDeactivated(ModifierKeys keys)
        {
            CurrentTerrain.ModificerDeactivated(keys);
        }
    }

    internal class EditorPropertyAttribute : Attribute
    {
    }
}
