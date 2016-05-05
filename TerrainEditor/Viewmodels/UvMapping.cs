using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using TerrainEditor.Utilities;

namespace TerrainEditor.ViewModels
{
    public class UvMapping : ViewModelBase
    {
        public class Segment
        {
            public List<Rect> Bodies;
            public Rect LeftCap;
            public Rect RightCap;
            public double ZOffset;
        }

        private Segment m_top;
        private Segment m_left;
        private Segment m_right;
        private Segment m_bottom;
        private BitmapImage m_edgeTexture;
        private BitmapImage m_fillTexture;

        public BitmapImage EdgeTexture
        {
            set
            {
                if (Equals(value, m_edgeTexture)) return;
                m_edgeTexture = value;
                OnPropertyChanged();
            }
            get { return m_edgeTexture; }
        }
        public BitmapImage FillTexture
        {
            set
            {
                if (Equals(value, m_fillTexture)) return;
                m_fillTexture = value;
                OnPropertyChanged();
            }
            get { return m_fillTexture; }
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
                EdgeTexture = Utils.LoadBitmapFromResource("Resources/pipe.png"),
                Top = new Segment
                {
                    LeftCap = new Rect(new Point(14, 0), new Size(50, 128)),
                    RightCap = new Rect(new Point(448, 0), new Size(50, 128)),
                    Bodies = new List<Rect>
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
                EdgeTexture = Utils.LoadBitmapFromResource("Resources/MossyEdges.png"),
                FillTexture = Utils.LoadBitmapFromResource("Resources/MossyFill.png"),
                Top = new Segment
                {
                    LeftCap = new Rect(new Point(0, 0), new Size(64, 220)),
                    RightCap = new Rect(new Point(448, 0), new Size(64, 220)),
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(64, 0), new Size(192, 220)),
                        new Rect(new Point(256, 0), new Size(192, 220)),
                    },
                    ZOffset = 0.04
                },
                Left = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(5, 231), new Size(232, 64)),
                    },
                    ZOffset = 0.01
                },
                Right = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(13, 231), new Size(218, 64)),
                    },
                    ZOffset = 0.02
                },
                Bottom = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(261, 199), new Size(231, 98)),
                    },
                    ZOffset = 0.03
                }
            };
        }

    }
}