using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using elios.Persist;
using TerrainEditor.Annotations;
using TerrainEditor.Utilities;

namespace TerrainEditor.ViewModels
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    [MetadataType(typeof(Rect))]
    public class RectMeta
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

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
        public Rect ToUv(Rect rect)
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
                    LeftCap = new Rect(new Point(14, 0), new Size(50, 128)),
                    RightCap = new Rect(new Point(448, 0), new Size(50, 128)),
                    Bodies = new ObservableCollection<Rect>
                    {
                        new Rect(new Point(64, 0), new Size(128, 128)),
                        new Rect(new Point(192, 0), new Size(128, 128)),
                        new Rect(new Point(320, 0), new Size(128, 128)),
                    },
                    ZOffset = 0.01
                }
            };

            Mossy = new UvMapping
            {
                Name = "Mossy",
                EdgeTexture = Utils.LoadBitmapFromResource("Resources/MossyEdges.png"),
                FillTexture = Utils.LoadBitmapFromResource("Resources/MossyFill.png"),
                Top = new Segment
                {
                    LeftCap = new Rect(new Point(0, 0), new Size(64, 220)),
                    RightCap = new Rect(new Point(448, 0), new Size(64, 220)),
                    Bodies = new ObservableCollection<Rect>
                    {
                        new Rect(new Point(64, 0), new Size(192, 220)),
                        new Rect(new Point(256, 0), new Size(192, 220)),
                    },
                    ZOffset = 0.04
                },
                Left = new Segment
                {
                    Bodies = new ObservableCollection<Rect>
                    {
                        new Rect(new Point(5, 231), new Size(232, 64)),
                    },
                    ZOffset = 0.01
                },
                Right = new Segment
                {
                    Bodies = new ObservableCollection<Rect>
                    {
                        new Rect(new Point(5, 231), new Size(232, 64)),
                    },
                    ZOffset = 0.02
                },
                Bottom = new Segment
                {
                    Bodies = new ObservableCollection<Rect>
                    {
                        new Rect(new Point(261, 199), new Size(232, 98)),
                    },
                    ZOffset = 0.03
                }
            };
        }

    }

    public class Segment : PropertyChangeBase
    {
        private SegmentEditor m_editor;
        private ObservableCollection<Rect> m_bodies;
        private Rect m_rightCap;
        private Rect m_leftCap;
        private double m_zOffset;

        public ObservableCollection<Rect> Bodies
        {
            get
            {
                return m_bodies;
            }
            set
            {
                if (Equals(value, m_bodies))
                    return;
                m_bodies = value;
                OnPropertyChanged();
            }
        }
        public Rect LeftCap
        {
            get
            {
                return m_leftCap;
            }
            set
            {
                if (value.Equals(m_leftCap))
                    return;
                m_leftCap = value;
                OnPropertyChanged();
            }
        }
        public Rect RightCap
        {
            get
            {
                return m_rightCap;
            }
            set
            {
                if (value.Equals(m_rightCap))
                    return;
                m_rightCap = value;
                OnPropertyChanged();
            }
        }
        public double ZOffset
        {
            get
            {
                return m_zOffset;
            }
            set
            {
                if (value.Equals(m_zOffset))
                    return;
                m_zOffset = value;
                OnPropertyChanged();
            }
        }

        [Persist(Ignore = true)]
        public SegmentEditor Editor
        {
            get { return m_editor ?? (m_editor = new SegmentEditor(this)); }
        }

        public Segment()
        {
            m_bodies = new ObservableCollection<Rect>();
        }
    }

    public class SegmentEditor : PropertyChangeBase
    {
        private readonly Segment m_segment;

        private Point m_position;
        private int m_height;
        private int m_capWidth;
        private int m_bodyWidth;
        private int m_bodySlices = 1;
        private bool m_isAdvanced = true;

        public SegmentEditor(Segment segment)
        {
            m_segment = segment;

            if (m_segment.Bodies.Count == 0)
                return;

            Size size = m_segment.Bodies[0].Size;

            m_isAdvanced = !( m_segment.LeftCap.Size == m_segment.RightCap.Size &&
                              m_segment.Bodies.All(s => s.Size == size) &&
                              m_segment.Bodies[0].Location.X - m_segment.LeftCap.Width == m_segment.LeftCap.Location.X );


            UpdateEditor();
        }

        public bool IsAdvanced
        {
            get
            {
                return m_isAdvanced;
            }
            set
            {
                if (value == m_isAdvanced)
                    return;
                m_isAdvanced = value;
                UpdateEditor();
                OnPropertyChanged();
            }
        }
        public Point Position
        {
            get
            {
                return m_position;
            }
            set
            {
                if (value.Equals(m_position))
                    return;
                m_position = value;
                UpdateSegment();
                OnPropertyChanged();
            }
        }
        public int Height
        {
            get
            {
                return m_height;
            }
            set
            {
                if (value == m_height)
                    return;
                m_height = value;
                UpdateSegment();
                OnPropertyChanged();
            }
        }
        public int CapWidth
        {
            get
            {
                return m_capWidth;
            }
            set
            {
                if (value == m_capWidth)
                    return;
                m_capWidth = value;
                UpdateSegment();
                OnPropertyChanged();
            }
        }
        public int BodyWidth
        {
            get
            {
                return m_bodyWidth;
            }
            set
            {
                if (value == m_bodyWidth)
                    return;
                m_bodyWidth = value;
                UpdateSegment();
                OnPropertyChanged();
            }
        }
        public int BodySlices
        {
            get
            {
                return m_bodySlices;
            }
            set
            {
                if (value == m_bodySlices)
                    return;
                m_bodySlices = value;
                UpdateSegment();
                OnPropertyChanged();
            }
        }

        private void UpdateSegment()
        {
            BodySlices = BodySlices < 1 ? 1 : BodySlices;
            m_segment.Bodies.Clear();

            Size bodySize = new Size(BodyWidth,Height);
            int startX = (int) ( CapWidth + Position.X );
            for (int i = 0; i < BodySlices; i++)
            {
                var bodyStartPos = new Point(startX,Position.Y);
                m_segment.Bodies.Add(new Rect(bodyStartPos, bodySize));
                startX += BodyWidth;
            }

            m_segment.LeftCap = new Rect(Position, new Size(CapWidth, Height));
            m_segment.RightCap = new Rect(new Point(Position.X + CapWidth + BodyWidth * BodySlices, Position.Y), new Size(CapWidth, Height));
        }
        private void UpdateEditor()
        {
            if (IsAdvanced== false)
            {
                m_position = m_segment.LeftCap.TopLeft;
                m_height = (int) m_segment.LeftCap.Height;
                m_capWidth = (int) m_segment.LeftCap.Width;
                m_bodyWidth = m_segment.Bodies.Count > 0 ? (int)m_segment.Bodies[0].Width : 0;
                m_bodySlices = m_segment.Bodies.Count;
            }
        }
    }

}