using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace TerrainEditor.Utilities
{
    public static class Utils
    {
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
        public static T CircularIndex<T>(this IReadOnlyList<T> source, int i, bool looped = false)
        {
            if (default(T) != null)
                throw new InvalidOperationException("CircularIndex<T> requires T to be a nullable type.");

            int n = source.Count;

            return i < 0 || i >= n ? (looped ? source[((i%n) + n)%n] : default(T)) : source[i];
        }
        public static T[] ToNewArray<T>(this T head)
        {
            return new[] {head};
        }


        public static Vector Normal(this Vector v)
        {
            var normal = new Vector(-v.Y,v.X);
            normal.Normalize();

            return normal;
        }

        public static Vector LinearLerp(Vector a, Vector b, double t)
        {
            return new Vector(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }
        public static Vector HermiteLerp(Vector a, Vector b, Vector c, Vector d, double percentage, double tension = 0, double bias = 0)
        {
            return new Vector(
                Hermite(a.X, b.X, c.X, d.X, percentage, tension, bias),
                Hermite(a.Y, b.Y, c.Y, d.Y, percentage, tension, bias));
        }
        private static double Hermite(double v1, double v2, double v3, double v4, double aPercentage, double aTension, double aBias)
        {
            double mu2 = aPercentage * aPercentage;
            double mu3 = mu2 * aPercentage;
            double m0 = (v2 - v1) * (1 + aBias) * (1 - aTension) / 2;
            m0 += (v3 - v2) * (1 - aBias) * (1 - aTension) / 2;
            double m1 = (v3 - v2) * (1 + aBias) * (1 - aTension) / 2;
            m1 += (v4 - v3) * (1 - aBias) * (1 - aTension) / 2;
            double a0 = 2 * mu3 - 3 * mu2 + 1;
            double a1 = mu3 - 2 * mu2 + aPercentage;
            double a2 = mu3 - mu2;
            double a3 = -2 * mu3 + 3 * mu2;

            return (a0 * v2 + a1 * m0 + a2 * m1 + a3 * v3);
        }

        public static void Clear(this MeshBuilder builder)
        {
            builder.Positions.Clear();
            builder.TriangleIndices.Clear();

            if (builder.CreateTextureCoordinates)
                builder.TextureCoordinates.Clear();

            if (builder.CreateNormals)
                builder.Normals.Clear();
        }

        public static DiffuseMaterial CreateImageMaterial(BitmapImage image, bool tile = false, bool freezeBrush = true)
        {
            ImageBrush imageBrush = new ImageBrush(image)
            {
                ViewportUnits = BrushMappingMode.Absolute,
                Opacity = 1,
                TileMode = tile ? TileMode.Tile : TileMode.None
            };

            if (freezeBrush)
                imageBrush.Freeze();

            DiffuseMaterial imageMaterial = new DiffuseMaterial(imageBrush);
            return imageMaterial;
        }
        public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
        }

        public static Point3D ToPoint3D(this Vector vector, double z = 0)
        {
            return new Point3D(vector.X,vector.Y,z);
        }
        public static Vector ToVector(this Point3D point,int decimalRounds = 2)
        {
            return new Vector(Math.Round(point.X, decimalRounds),Math.Round(point.Y,decimalRounds));
        }
    }
}
