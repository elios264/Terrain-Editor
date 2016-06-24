using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TerrainEditor.UserControls.UvMappingControls
{
    public class MoveResizeThumb : Thumb
    {
        public static readonly DependencyProperty SizeProperty;
        public static readonly DependencyProperty LocationProperty;
        public static readonly DependencyProperty RectProperty;

        private static readonly DependencyPropertyKey RectPropertyKey;

        public Rect Rect
        {
            get { return (Rect)GetValue(RectProperty); }
            private set { SetValue(RectPropertyKey, value); }
        }
        public Point Location
        {
            get { return (Point)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }
        public Size Size
        {
            get { return (Size)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private Point m_startPoint;

        static MoveResizeThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MoveResizeThumb), new FrameworkPropertyMetadata(typeof(MoveResizeThumb)));
            SizeProperty = DependencyProperty.Register(nameof(Size), typeof(Size), typeof(MoveResizeThumb), new FrameworkPropertyMetadata(default(Size),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, args) => (o as MoveResizeThumb).Tesellate()));
            LocationProperty = DependencyProperty.Register(nameof(Location), typeof(Point), typeof(MoveResizeThumb), new FrameworkPropertyMetadata(default(Point),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, args) => (o as MoveResizeThumb).Tesellate()));
            RectPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Rect), typeof(Rect), typeof(MoveResizeThumb), new PropertyMetadata(default(Rect)));
            RectProperty = RectPropertyKey.DependencyProperty;
        }
        public MoveResizeThumb()
        {
            DragStarted += (sender, args) => m_startPoint = Location;

            DragDelta += (_,args) =>
            {
                Location = new Point(m_startPoint.X + Math.Round(args.HorizontalChange), m_startPoint.Y + Math.Round(args.VerticalChange));
                Tesellate();
            };
        }

        private void Tesellate()
        {
            Rect = new Rect(Location, Size);
        }
    }
    public class ResizeThumb : Thumb
    {
        public static readonly DependencyProperty MoveResizeThumbProperty;

        public MoveResizeThumb MoveResizeThumb
        {
            get { return (MoveResizeThumb)GetValue(MoveResizeThumbProperty); }
            set { SetValue(MoveResizeThumbProperty, value); }
        }

        static ResizeThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeThumb), new FrameworkPropertyMetadata(typeof(ResizeThumb)));
            MoveResizeThumbProperty = DependencyProperty.Register(nameof(MoveResizeThumb), typeof(MoveResizeThumb), typeof(ResizeThumb), new PropertyMetadata(default(MoveResizeThumb)));
        }
        public ResizeThumb()
        {
            DragDelta += OnDragDelta;
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (MoveResizeThumb == null)
                return;

            if (VerticalAlignment == VerticalAlignment.Bottom || VerticalAlignment == VerticalAlignment.Top)
            {
                var stateSize = MoveResizeThumb.Size;
                var deltaVertical = Math.Round(Math.Min((VerticalAlignment == VerticalAlignment.Bottom
                    ? -1
                    : 1) * e.VerticalChange, stateSize.Height /*- stateItem.MinHeight*/));

                MoveResizeThumb.Size =  new Size(stateSize.Width, stateSize.Height - deltaVertical);

                if (VerticalAlignment == VerticalAlignment.Top)
                {
                    var statePosition = MoveResizeThumb.Location;
                    MoveResizeThumb.Location = new Point(statePosition.X, statePosition.Y + deltaVertical);
                }
            }

            if (HorizontalAlignment == HorizontalAlignment.Left || HorizontalAlignment == HorizontalAlignment.Right)
            {
                var stateSize = MoveResizeThumb.Size;
                var deltaHorizontal = Math.Round(Math.Min((HorizontalAlignment == HorizontalAlignment.Right
                    ? -1
                    : 1) * e.HorizontalChange, stateSize.Width /* - stateItem.MinWidth*/));

                MoveResizeThumb.Size = new Size(stateSize.Width - deltaHorizontal, stateSize.Height);

                if (HorizontalAlignment == HorizontalAlignment.Left)
                {
                    Point statePosition = MoveResizeThumb.Location;
                    MoveResizeThumb.Location = new Point(statePosition.X + deltaHorizontal, statePosition.Y);
                }
            }

            e.Handled = true;
        }
    }
}
