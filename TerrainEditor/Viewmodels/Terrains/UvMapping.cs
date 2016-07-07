using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Core;
using TerrainEditor.Utilities;
using Urho;

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
            float width = EdgeTexture.PixelWidth;
            float height = EdgeTexture.PixelHeight;

            rect.Min.X = rect.Min.X/width;
            rect.Min.Y = rect.Min.Y/height;

            rect.Max.X = rect.Max.X/width;
            rect.Max.Y = rect.Max.Y/height;

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
                    Offsets = new Vector3(0,0,0.01f),
                    CapSize = new Vector2(50, 128),
                    BodySize = new Vector2(128, 128),

                    LeftCap = new Vector2(14,0),
                    RightCap = new Vector2(448,0),

                    Bodies = new ObservableCollection<Vector2>
                    {
                        new Vector2(64, 0),
                        new Vector2(192, 0),
                        new Vector2(320, 0)
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
                    Offsets = new Vector3(0, 0, 0.04f),
                    CapSize = new Vector2(64, 220),
                    BodySize = new Vector2(192, 220),

                    LeftCap = new Vector2(0, 0),
                    RightCap = new Vector2(448, 0),
                    Bodies = new ObservableCollection<Vector2> { new Vector2(64,0), new Vector2(256,0) }
                },
                Left = new Segment
                {
                    Offsets = new Vector3(0, 0, 0.01f),
                    BodySize = new Vector2(232, 64),
                    Bodies = new ObservableCollection<Vector2> { new Vector2(5, 231) }
                },
                Right = new Segment
                {
                    Offsets = new Vector3(0, 0, 0.02f),
                    BodySize = new Vector2(232, 64),
                    Bodies = new ObservableCollection<Vector2> { new Vector2(5, 231) },
                },
                Bottom = new Segment
                {
                    Offsets = new Vector3(0, 0, 0.03f),
                    BodySize = new Vector2(232, 98),
                    Bodies = new ObservableCollection<Vector2> { new Vector2(261, 199) }
                }
            };
        }
    }

    public class Segment : PropertyChangeBase
    {
        private Vector2 m_capSize;
        private Vector2 m_bodySize;
        private Vector2 m_leftCap;
        private Vector2 m_rightCap;
        private Vector3 m_offsets;
        private ObservableCollection<Vector2> m_bodies = new ObservableCollection<Vector2>();

        public Vector3 Offsets
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
        public Vector2 CapSize
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
        public Vector2 BodySize
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
        public Vector2 LeftCap
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
        public Vector2 RightCap
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
        public ObservableCollection<Vector2> Bodies
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

    [MetadataType(typeof(Vector2))]
    [TypeConverter(typeof(Vector234Converter))]
    public class Vector2Meta {}
    [MetadataType(typeof(Vector3))]
    [TypeConverter(typeof(Vector234Converter))]
    public class Vector3Meta {}

    public class Vector234Converter : TypeConverter
    {
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
            var str = value as string;

            if (str != null)
            {
                var values = str.Split(',');
                switch (values.Length)
                {
                case 2:
                    return new Vector2(float.Parse(values[0], culture), float.Parse(values[1], culture));
                case 3:
                    return new Vector3(float.Parse(values[0], culture), float.Parse(values[1], culture),
                        float.Parse(values[2], culture));
                case 4:
                    return new Vector4(float.Parse(values[0], culture), float.Parse(values[1], culture),
                        float.Parse(values[2], culture), float.Parse(values[3], culture));
                }
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Vector2)
            {
                var v2 = (Vector2) value;
                return string.Format(culture, "{0},{1}", v2.X, v2.Y);
            }
            if (value is Vector3)
            {
                var v3 = (Vector3)value;
                return string.Format(culture, "{0},{1},{2}", v3.X, v3.Y, v3.Z);
            }
            if (value is Vector4)
            {
                var v4 = (Vector4)value;
                return string.Format(culture, "{0},{1},{2},{3}", v4.X, v4.Y, v4.Z, v4.W);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}