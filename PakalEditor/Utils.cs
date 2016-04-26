using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Strilanc.Value;

namespace PakalEditor
{

    public static class Utils
    {
        public static readonly Random Random = new Random();

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
        public static DiffuseMaterial CreateImageMaterial(BitmapImage image, bool tile = false, bool freezeBrush = true)
        {
            ImageBrush imageBrush = new ImageBrush(image)
            {
                ViewportUnits = BrushMappingMode.Absolute,
                Opacity = 1,
                TileMode = tile ?  TileMode.Tile : TileMode.None
            };

            if (freezeBrush)
                imageBrush.Freeze();

            DiffuseMaterial imageMaterial = new DiffuseMaterial(imageBrush);
            return imageMaterial;
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static void EnsureNotNull<T>(T test) where  T: class
        {
            if (test == null)
            {
                throw new NullReferenceException($"{typeof (T).Name} is null");
            }
        }

        public static double ToRadians(double degrees)
        {
            return degrees/(180/Math.PI);
        }
        public static double ToDegrees(double radians) 
        {
            return radians*(180/Math.PI);
        }
    }

    public static class VectorUtils
    {
        public static Vector3D Normalized(this Vector3D vector)
        {
            vector.Normalize();
            return vector;
        }
        public static Vector Normalized(this Vector vector)
        {
            vector.Normalize();
            return vector;
        }

        public static Vector3D NormalXZ(Point3D vector, Point3D origin = new Point3D() )
        {
            Vector3D direction = vector - origin;

            return new Vector3D(-direction.Z, direction.Y, direction.X).Normalized();
        }
        public static Vector3D RotateVectorXZ(Vector3D vector, double degrees)
        {
            double radians = Utils.ToRadians(degrees);

            return new Vector3D
            {
                X = vector.X * Math.Cos(radians) - vector.Z * Math.Sin(radians),
                Y = vector.Y,
                Z = vector.X * Math.Sin(radians) + vector.Z * Math.Cos(radians)
            };
        }

        public static Vector ToVectorXZ(this Vector3D vector)
        {
            return new Vector(vector.X, vector.Z);
        }
        public static Vector ToVectorXZ(this Point3D vector)
        {
            return new Vector(vector.X, vector.Z);
        }
        public static Point ToPointXZ(this Point3D point)
        {
            return new Point(point.X, point.Z);
        }
        public static Point3D ToPointXZ(this Point point, double y = 0)
        {
            return new Point3D(point.X,y,point.Y);
        }

        public static Vector3D LinearLerp(Vector3D a, Vector3D b, double t)
        {
            return new Vector3D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        }
        public static Point3D LinearLerp(Point3D a, Point3D b, double t)
        {
            return new Point3D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        }

        public static Point3D HermiteLerp(Point3D a, Point3D b, Point3D c, Point3D d, double percentage, double tension = 0, double bias = 0)
        {
            return  new Point3D(
                Hermite(a.X,b.X,c.X,d.X,percentage,tension,bias),
                Hermite(a.Y,b.Y,c.Y,d.Y,percentage,tension,bias),
                Hermite(a.Z,b.Z,c.Z,d.Z,percentage,tension,bias)
                );
        }
        public static Point3D CubicLerp(Point3D a, Point3D b, Point3D c, Point3D d, double percentage)
        {
            return  new Point3D(
                Cubic(a.X,b.X,c.X,d.X,percentage),
                Cubic(a.Y,b.Y,c.Y,d.Y,percentage),
                Cubic(a.Z,b.Z,c.Z,d.Z,percentage)
                );
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
        private static double Cubic(double v1, double v2, double v3, double v4, double aPercentage)
        {
            double percentageSquared = aPercentage * aPercentage;
            double a1 = v4 - v3 - v1 + v2;
            double a2 = v1 - v2 - a1;
            double a3 = v3 - v1;
            double a4 = v2;

            return (a1 * aPercentage * percentageSquared + a2 * percentageSquared + a3 * aPercentage + a4);
        }
    }

    public static class QuaternionUtils
    {
        public static Quaternion CreateFromAxisAngle(Vector3D axis, double angle)
        {
            double num2 = angle * 0.5f;
            double num = Math.Sin(num2);
            double num3 = Math.Cos(num2);

            Quaternion quaternion = new Quaternion
            {
                X = axis.X*num,
                Y = axis.Y*num,
                Z = axis.Z*num,
                W = num3
            };

            return quaternion;
        }

        public static Quaternion CreateFromRotationMatrix(Matrix3D matrix)
        {
            double num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            Quaternion quaternion = new Quaternion();
            if (num8 > 0f)
            {
                double num = Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (matrix.M23 - matrix.M32) * num;
                quaternion.Y = (matrix.M31 - matrix.M13) * num;
                quaternion.Z = (matrix.M12 - matrix.M21) * num;
                return quaternion;
            }
            if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                double num7 = Math.Sqrt(((1f + matrix.M11) - matrix.M22) - matrix.M33);
                double num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (matrix.M12 + matrix.M21) * num4;
                quaternion.Z = (matrix.M13 + matrix.M31) * num4;
                quaternion.W = (matrix.M23 - matrix.M32) * num4;
                return quaternion;
            }
            if (matrix.M22 > matrix.M33)
            {
                double num6 = Math.Sqrt(((1f + matrix.M22) - matrix.M11) - matrix.M33);
                double num3 = 0.5f / num6;
                quaternion.X = (matrix.M21 + matrix.M12) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (matrix.M32 + matrix.M23) * num3;
                quaternion.W = (matrix.M31 - matrix.M13) * num3;
                return quaternion;
            }
            double num5 = Math.Sqrt(((1f + matrix.M33) - matrix.M11) - matrix.M22);
            double num2 = 0.5f / num5;
            quaternion.X = (matrix.M31 + matrix.M13) * num2;
            quaternion.Y = (matrix.M32 + matrix.M23) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (matrix.M12 - matrix.M21) * num2;

            return quaternion;

        }

        public static Quaternion CreateFromRotationAxes(Vector3D xaxis,Vector3D yaxis,Vector3D zaxis)
        {
            Matrix3D kRot = new Matrix3D
            {
                M11 = xaxis.X,
                M12 = xaxis.Y,
                M13 = xaxis.Z,
                M21 = yaxis.X,
                M22 = yaxis.Y,
                M23 = yaxis.Z,
                M31 = zaxis.X,
                M32 = zaxis.Y,
                M33 = zaxis.Z
            };

            return CreateFromRotationMatrix(kRot);
        }

        public static Quaternion CreateFromYawPitchRoll(double yaw, double pitch, double roll)
        {
            double num9 = roll * 0.5f;
            double num6 = Math.Sin(num9);
            double num5 = Math.Cos(num9);
            double num8 = pitch * 0.5f;
            double num4 = Math.Sin(num8);
            double num3 = Math.Cos(num8);
            double num7 = yaw * 0.5f;
            double num2 = Math.Sin(num7);
            double num = Math.Cos(num7);
            Quaternion quaternion = new Quaternion
            {
                X = ((num*num4)*num5) + ((num2*num3)*num6),
                Y = ((num2*num3)*num5) - ((num*num4)*num6),
                Z = ((num*num3)*num6) - ((num2*num4)*num5),
                W = ((num*num3)*num5) + ((num2*num4)*num6)
            };
            return quaternion;
        }
    }

    public static class LinqUtils
    {
        public static May<T> At<T>(this IReadOnlyList<T> source,int i, bool looped = false)
        {
            int n = source.Count;

            if (i >= n)
            {
                return looped ? source[((i % n) + n) % n] : May<T>.NoValue;
            }
            if (i < 0)
            {
                return looped ? source[((i % n) + n) % n] : May<T>.NoValue;
            }

            return source[i];
        }
    }

}