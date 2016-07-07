using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using Urho;
using Rect = System.Windows.Rect;

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
        public Vector2 Location
        {
            get { return (Vector2)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }
        public Vector2 Size
        {
            get { return (Vector2)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private Vector2 m_startPoint;

        static MoveResizeThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MoveResizeThumb), new FrameworkPropertyMetadata(typeof(MoveResizeThumb)));
            SizeProperty = DependencyProperty.Register(nameof(Size), typeof(Vector2), typeof(MoveResizeThumb), new FrameworkPropertyMetadata(default(Vector2),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, args) => (o as MoveResizeThumb).Tesellate()));
            LocationProperty = DependencyProperty.Register(nameof(Location), typeof(Vector2), typeof(MoveResizeThumb), new FrameworkPropertyMetadata(default(Vector2),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (o, args) => (o as MoveResizeThumb).Tesellate()));
            RectPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Rect), typeof(Rect), typeof(MoveResizeThumb), new PropertyMetadata(default(Rect)));
            RectProperty = RectPropertyKey.DependencyProperty;
        }
        public MoveResizeThumb()
        {
            DragStarted += (sender, args) => m_startPoint = Location;

            DragDelta += (_,args) =>
            {
                Location = new Vector2((float) (m_startPoint.X + Math.Round(args.HorizontalChange)), (float) (m_startPoint.Y + Math.Round(args.VerticalChange)));
                Tesellate();
            };
        }

        private void Tesellate()
        {
            Rect = new Rect(new Point(Location.X, Location.Y), new Size(Size.X, Size.Y));
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
                var deltaVertical = (float)Math.Round(Math.Min((VerticalAlignment == VerticalAlignment.Bottom
                    ? -1
                    : 1) * e.VerticalChange, stateSize.Y /*- stateItem.MinHeight*/));

                MoveResizeThumb.Size =  new Vector2(stateSize.X, stateSize.Y - deltaVertical);

                if (VerticalAlignment == VerticalAlignment.Top)
                {
                    var statePosition = MoveResizeThumb.Location;
                    MoveResizeThumb.Location = new Vector2(statePosition.X, statePosition.Y + deltaVertical);
                }
            }

            if (HorizontalAlignment == HorizontalAlignment.Left || HorizontalAlignment == HorizontalAlignment.Right)
            {
                var stateSize = MoveResizeThumb.Size;
                var deltaHorizontal = (float)Math.Round(Math.Min((HorizontalAlignment == HorizontalAlignment.Right
                    ? -1
                    : 1) * e.HorizontalChange, stateSize.X /* - stateItem.MinWidth*/));

                MoveResizeThumb.Size = new Vector2(stateSize.X - deltaHorizontal, stateSize.Y);

                if (HorizontalAlignment == HorizontalAlignment.Left)
                {
                    var statePosition = MoveResizeThumb.Location;
                    MoveResizeThumb.Location = new Vector2(statePosition.X + deltaHorizontal, statePosition.Y);
                }
            }

            e.Handled = true;
        }
    }
}
