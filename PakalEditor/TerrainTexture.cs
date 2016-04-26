using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Strilanc.Value;

namespace PakalEditor
{
    public class TerrainTexture
    {
        public struct Segment
        {
            public List<Rect> Bodies;
            public Rect LeftCap;
            public Rect RightCap;
            public double ZOffset;

            public bool Valid
            {
                get { return Bodies != null && Bodies.Count > 0; }
            }
        }

        public static TerrainTexture Pipe;
        public static TerrainTexture Mossy;

        public BitmapImage EdgeTexture;
        public BitmapImage FillTexture;
        public Segment Top;
        public May<Segment> Left;
        public May<Segment> Right;
        public May<Segment> Bottom;

        static TerrainTexture()
        {
            Pipe = new TerrainTexture
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
                    ZOffset = -0.01
                }
            };

            Mossy = new TerrainTexture
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
                    ZOffset = -0.04
                },
                Left = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(5, 231), new Size(232, 64)),
                    },
                    ZOffset = -0.01
                },
                Right = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(13, 231), new Size(218, 64)),
                    },
                    ZOffset = -0.02
                },
                Bottom = new Segment
                {
                    Bodies = new List<Rect>
                    {
                        new Rect(new Point(261, 199), new Size(231, 98)),
                    },
                    ZOffset = -0.03
                }
            };
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

        public double FindFrontestOffset()
        {
            return new[]
            {
                Top.ZOffset,
                Left.Select(s => s.ZOffset),
                Right.Select(s => s.ZOffset),
                Bottom.Select(s => s.ZOffset),
            }.WhereHasValue().Min();
        }
    }
}