using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using elios.Persist;
using TerrainEditor.Core;
using TerrainEditor.Utilities;

namespace TerrainEditor.Viewmodels.Terrains
{
    public class UvMapping : PropertyChangeBase
    {
        private string m_name;
        private Segment m_top = new Segment();
        private Segment m_left;
        private Segment m_right;
        private Segment m_bottom;
        private Lazy<BitmapImage> m_edgeTexture;
        private Lazy<BitmapImage> m_fillTexture;

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                if (value == m_name)
                    return;
                m_name = value;
                OnPropertyChanged();
            }
        }

        [Persist(nameof(EdgeTexture))]
        public string EdgeTexturePath
        {
            get
            {
                return EdgeTexture?.UriSource?.OriginalString;
            }
            set
            {
                if (EdgeTexturePath == value) return;

                m_edgeTexture = new Lazy<BitmapImage>(() =>
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(value,UriKind.RelativeOrAbsolute);
                    img.EndInit();
                    img.Freeze();
                    return img;
                });

                OnPropertyChanged(nameof(EdgeTexturePath));
                OnPropertyChanged(nameof(EdgeTexture));

            }
        }
        [Persist(nameof(FillTexture))]
        public string FillTexturePath
        {
            get
            {
                return FillTexture?.UriSource?.OriginalString;
            }
            set
            {
                if (FillTexturePath == value) return;

                m_fillTexture = new Lazy<BitmapImage>(() =>
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(value,UriKind.RelativeOrAbsolute);
                    img.EndInit();
                    img.Freeze();
                    return img;
                });

                OnPropertyChanged(nameof(FillTexturePath));
                OnPropertyChanged(nameof(FillTexture));
            }
        }

        [Persist(Ignore = true)]
        public BitmapImage EdgeTexture
        {
            set
            {
                if (Equals(value, m_edgeTexture?.Value)) return;
                m_edgeTexture = new Lazy<BitmapImage>(() => value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(EdgeTexturePath));
            }
            get { return m_edgeTexture?.Value; }
        }
        [Persist(Ignore = true)]
        public BitmapImage FillTexture
        {
            set
            {
                if (Equals(value, m_fillTexture?.Value)) return;
                m_fillTexture = new Lazy<BitmapImage>(() => value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(FillTexturePath));
            }
            get { return m_fillTexture?.Value; }
        }

        public Segment Top
        {
            set
            {
                if (Equals(value, m_top)) return;
                m_top = value;
                OnPropertyChanged();
            }
            get { return m_top; }
        }
        public Segment Left
        {
            set
            {
                if (Equals(value, m_left)) return;
                m_left = value;
                OnPropertyChanged();
            }
            get { return m_left; }
        }
        public Segment Right
        {
            set
            {
                if (Equals(value, m_right)) return;
                m_right = value;
                OnPropertyChanged();
            }
            get { return m_right; }
        }
        public Segment Bottom
        {
            set
            {
                if (Equals(value, m_bottom)) return;
                m_bottom = value;
                OnPropertyChanged();
            }
            get { return m_bottom; }
        }
        public Rect ToUV(Rect rect)
        {
            double width = EdgeTexture.PixelWidth;
            double height = EdgeTexture.PixelHeight;

            rect.X /= width;
            rect.Y /= height;

            rect.Width /= width;
            rect.Height /= height;

            return rect;
        }


        public static readonly UvMapping Pipe;
        public static readonly UvMapping Mossy;

        static UvMapping()
        {
            Pipe = new UvMapping
            {
                Name = "Pipe",
                EdgeTexture = Utils.LoadBitmapFromResource("Resources/pipe.png"),
                Top = new Segment
                {
                    Offsets = new Vector3D(0,0,0.01),
                    CapSize = new Size(50, 128),
                    BodySize = new Size(128, 128),

                    LeftCap = new Point(14,0),
                    RightCap = new Point(448,0),

                    Bodies = new ObservableCollection<Point>
                    {
                        new Point(64, 0),
                        new Point(192, 0),
                        new Point(320, 0)
                    }
                }
            };

            Mossy = new UvMapping
            {
                Name = "Mossy",
                EdgeTexture = Utils.LoadBitmapFromResource("Resources/MossyEdges.png"),
                FillTexture = Utils.LoadBitmapFromResource("Resources/MossyFill.png"),
                Top = new Segment
                {
                    Offsets = new Vector3D(0, 0, 0.04),
                    CapSize = new Size(64, 220),
                    BodySize = new Size(192, 220),

                    LeftCap = new Point(0, 0),
                    RightCap = new Point(448, 0),
                    Bodies = new ObservableCollection<Point> { new Point(64,0), new Point(256,0) }
                },
                Left = new Segment
                {
                    Offsets = new Vector3D(0, 0, 0.01),
                    BodySize = new Size(232, 64),
                    Bodies = new ObservableCollection<Point> { new  Point(5, 231) }
                },
                Right = new Segment
                {
                    Offsets = new Vector3D(0, 0, 0.02),
                    BodySize = new Size(232, 64),
                    Bodies = new ObservableCollection<Point> { new Point(5, 231) },
                },
                Bottom = new Segment
                {
                    Offsets = new Vector3D(0, 0, 0.03),
                    BodySize = new Size(232, 98),
                    Bodies = new ObservableCollection<Point> { new Point(261, 199) }
                }
            };
        }
    }

    public class Segment : PropertyChangeBase
    {
        private Size m_capSize;
        private Size m_bodySize;
        private Point m_leftCap;
        private Point m_rightCap;
        private Vector3D m_offsets;
        private ObservableCollection<Point> m_bodies = new ObservableCollection<Point>();

        public Vector3D Offsets
        {
            get { return m_offsets; }
            set
            {
                if (value.Equals(m_offsets))
                    return;
                m_offsets = value;
                OnPropertyChanged();
            }
        }
        public Size CapSize
        {
            get { return m_capSize; }
            set
            {
                if (value.Equals(m_capSize))
                    return;
                m_capSize = value;
                OnPropertyChanged();
            }
        }
        public Size BodySize
        {
            get { return m_bodySize; }
            set
            {
                if (value.Equals(m_bodySize))
                    return;
                m_bodySize = value;
                OnPropertyChanged();
            }
        }
        public Point LeftCap
        {
            get { return m_leftCap; }
            set
            {
                if (value.Equals(m_leftCap))
                    return;
                m_leftCap = value;
                OnPropertyChanged();
            }
        }
        public Point RightCap
        {
            get { return m_rightCap; }
            set
            {
                if (value.Equals(m_rightCap))
                    return;
                m_rightCap = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Point> Bodies
        {
            get { return m_bodies; }
            set
            {
                if (Equals(value, m_bodies))
                    return;
                m_bodies = value;
                OnPropertyChanged();
            }
        }
    }

    /*[TypeConverter(typeof(PointConverter))]
    public class Point : PropertyChangeBase
    {
        private double m_x;
        private double m_y;

        public double X
        {
            get { return m_x; }
            set
            {
                if (value.Equals(m_x))
                    return;
                m_x = value;
                OnPropertyChanged();
            }
        }
        public double Y
        {
            get { return m_y; }
            set
            {
                if (value.Equals(m_y))
                    return;
                m_y = value;
                OnPropertyChanged();
            }
        }

        public Point() {}
        public Point(double x, double y)
        {
            m_x = x;
            m_y = y;
        }

        public static implicit operator System.Windows.Point(Point point)
        {
            return point == null
                ? default(System.Windows.Point)
                : new System.Windows.Point(point.X,point.Y);
        }

        public override string ToString()
        {
            return ((System.Windows.Point)this).ToString();
        }
 

        public class PointConverter : TypeConverter
        {
            private static readonly System.Windows.PointConverter Pconverter = new System.Windows.PointConverter();

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                var p = (System.Windows.Point)Pconverter.ConvertFrom(context, culture, value);

                return new Point { X = p.X, Y = p.Y };
            }
            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                var point = (Point)value ?? new Point();
                return Pconverter.ConvertTo(context, culture, new System.Windows.Point(point.X, point.Y), destinationType);
            }
        }
    }*/
}