using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using TerrainEditor.Utilities;

namespace TerrainEditor.UserControls
{
    public class PositionManipulator : ModelVisual3D
    {
        public static readonly DependencyProperty TargetPositionProperty = DependencyProperty.Register(
            nameof(TargetPosition), typeof(Point3D), typeof(PositionManipulator), new FrameworkPropertyMetadata(default(Point3D), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, _) => (o as PositionManipulator).Tesellate()));
        public static readonly DependencyProperty TargetRotationZProperty = DependencyProperty.Register(
            nameof(TargetRotationZ), typeof(double), typeof(PositionManipulator), new FrameworkPropertyMetadata(default(double),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, _) => (o as PositionManipulator).Tesellate()));
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point3D), typeof(PositionManipulator), new PropertyMetadata(default(Point3D), (o, _) => (o as PositionManipulator).Tesellate()));
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
            nameof(Length), typeof(double), typeof(PositionManipulator), new UIPropertyMetadata(2.0, (o, _) => (o as PositionManipulator).Tesellate()));
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
            nameof(Diameter), typeof(double), typeof(PositionManipulator), new UIPropertyMetadata(0.2, (o, _) => (o as PositionManipulator).Tesellate()));

  
        //Can translate x,y,z, Can Rotate z

        public Point3D Position
        {
            get { return (Point3D)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }
        public double TargetRotationZ
        {
            get { return (double)GetValue(TargetRotationZProperty); }
            set { SetValue(TargetRotationZProperty, value); }
        }
        public Point3D TargetPosition
        {
            get { return (Point3D)GetValue(TargetPositionProperty); }
            set { SetValue(TargetPositionProperty, value); }
        }
        public double Length
        {
            get { return (double)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }
        public double Diameter
        {
            get { return (double)GetValue(DiameterProperty); }
            set { SetValue(DiameterProperty, value); }
        }

        private readonly GeometryUIModel m_yArrow;
        private readonly GeometryUIModel m_xArrow;
        private readonly GeometryUIModel m_zArrow;
        private readonly GeometryUIModel m_zPipe;

        private Vector3D m_offset;
        private Point3D m_lastPoint;
        private double m_startRotation;

        private static readonly Material RedMat;
        private static readonly Material BlueMat;
        private static readonly Material GreenMat;
        private static readonly Material GoldMat;

        static PositionManipulator()
        {
            RedMat = MaterialHelper.CreateMaterial(Colors.Red);
            BlueMat = MaterialHelper.CreateMaterial(Colors.Blue);
            GreenMat = MaterialHelper.CreateMaterial(Colors.Green);
            GoldMat = MaterialHelper.CreateMaterial(Colors.Gold);
            RedMat.Freeze();
            BlueMat.Freeze();
            GreenMat.Freeze();
            GoldMat.Freeze();
        }
        public PositionManipulator()
        {
            m_xArrow = new GeometryUIModel();
            m_yArrow = new GeometryUIModel();
            m_zArrow = new GeometryUIModel();
            m_zPipe = new GeometryUIModel();

            Children.Add(m_xArrow);
            Children.Add(m_yArrow);
            Children.Add(m_zArrow);
            Children.Add(m_zPipe);

            m_yArrow.MouseDown += ArrowOnMouseDown;
            m_xArrow.MouseDown += ArrowOnMouseDown;
            m_zArrow.MouseDown += ArrowOnMouseDown;

            m_yArrow.MouseMove += ArrowOnMouseMove;
            m_xArrow.MouseMove += ArrowOnMouseMove;
            m_zArrow.MouseMove += ArrowOnMouseMove;

            m_yArrow.MouseUp += OnUiModelMouseUp;
            m_xArrow.MouseUp += OnUiModelMouseUp;
            m_zArrow.MouseUp += OnUiModelMouseUp;

            m_zPipe.MouseDown += OnPipeMouseDown;
            m_zPipe.MouseMove += OnPipeMouseMove;
            m_zPipe.MouseUp += OnUiModelMouseUp;
        }

        public void Tesellate()
        {
            var builder = new MeshBuilder(false, false);
            builder.AddArrow(Position, Position + new Vector3D(0, 1, 0) * Length, Diameter);

            m_yArrow.Model = new GeometryModel3D(builder.ToMesh(true), RedMat) { BackMaterial = RedMat };

           // builder.Clear();
            builder.AddArrow(Position, Position + new Vector3D(1, 0, 0) * Length, Diameter);

            m_xArrow.Model = new GeometryModel3D(builder.ToMesh(true), BlueMat) { BackMaterial = BlueMat };

         //   builder.Clear();
            builder.AddArrow(Position, Position + new Vector3D(-0.70, -0.70, 0) * Length, Diameter);

            m_zArrow.Model = new GeometryModel3D(builder.ToMesh(true), GreenMat) { BackMaterial = GreenMat };

          //  builder.Clear();
            var pos = Position;
            var axis = new Vector3D(0,0,1);
            builder.AddPipe(pos - axis * 0.1 * 0.5, pos + axis * 0.1 * 0.5, 2.4, 2.7, 60);

            m_zPipe.Model = new GeometryModel3D(builder.ToMesh(true), GoldMat) { BackMaterial = GoldMat };
        }

        private void OnPipeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_zPipe.CaptureMouse();
                m_lastPoint = ScreenPointToWorld(e.GetPosition(m_zPipe));
                m_startRotation = TargetRotationZ;
            }
        }

        private void OnPipeMouseMove(object sender, MouseEventArgs e)
        {
            if (m_zPipe.IsMouseCaptured)
            {
                var v = m_lastPoint - Position;
                var u = ScreenPointToWorld(e.GetPosition(m_zPipe)) - Position;
                var angle = Vector3D.AngleBetween(v, u);

                TargetRotationZ = Math.Round((Math.Sign(Vector3D.CrossProduct(v, u).Z) > 0 ? angle : -angle) + m_startRotation,2);
            }
        }
        private void ArrowOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var arrow = (GeometryUIModel)sender;
                arrow.CaptureMouse();
                m_offset = TargetPosition - ScreenPointToWorld(e.GetPosition(arrow));
                m_offset.Z = TargetPosition.Z;
            }
        }
        private void ArrowOnMouseMove(object sender, MouseEventArgs e)
        {
            var arrow = (GeometryUIModel)sender;

            if (arrow.IsMouseCaptured)
            {
                var position = ScreenPointToWorld(e.GetPosition(arrow));

                if (arrow == m_yArrow)
                    TargetPosition = new Point3D(TargetPosition.X, Math.Round(position.Y + m_offset.Y,1), TargetPosition.Z);
                else if (arrow == m_xArrow)
                    TargetPosition = new Point3D(Math.Round(position.X + m_offset.X, 1), TargetPosition.Y, TargetPosition.Z);
                else if (arrow == m_zArrow)
                    TargetPosition = new Point3D(TargetPosition.X, TargetPosition.Y , Math.Round((position.X + m_offset.X)/2 + m_offset.Z, 2));
            }
        }
        private static void OnUiModelMouseUp(object sender, MouseButtonEventArgs args)
        {
            ((GeometryUIModel)sender).ReleaseMouseCapture();
        }

        private Point3D ScreenPointToWorld(Point point)
        {
            var ray = this.GetViewport3D().Point2DtoRay3D(point);
            var swp = ray.PlaneIntersection(TargetPosition, new Vector3D(0, 0, -1)).Value;

            return Transform.Inverse.Transform(swp);
        }

        private class GeometryUIModel : UIElement3D
        {
            public GeometryModel3D Model
            {
                get { return (GeometryModel3D)Visual3DModel; }
                set { Visual3DModel = value; }
            }
        }
    }
}