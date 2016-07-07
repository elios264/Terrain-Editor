using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Urho;

namespace TerrainEditor.Utilities
{
    public static class Utils
    {
        internal static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
        internal static T At<T>(this IList<T> source, int i, bool looped = false)
        {
            if (default(T) != null)
                throw new ArgumentException("At<T> requires T to be a nullable type.",nameof(T));

            int n = source.Count;
            return i < 0 || i >= n ? (looped ? source[((i%n) + n)%n] : default(T)) : source[i];
        }
        internal static Vector2 Normal(this Vector2 v)
        {
            var normal = new Vector2(-v.Y,v.X);
            normal.Normalize();

            return normal;
        }
        internal static Vector2 LinearInterpolate(Vector2 a, Vector2 b, float percentaje)
        {
            return new Vector2(a.X + (b.X - a.X) * percentaje, a.Y + (b.Y - a.Y) * percentaje);
        }
        internal static Vector2 HermiteInterpolate(Vector2 prev, Vector2 begin, Vector2 end, Vector2 next, float percentage, float tension = 0, float bias = 0)
        {
            return new Vector2(
                Hermite(prev.X, begin.X, end.X, next.X, percentage, tension, bias),
                Hermite(prev.Y, begin.Y, end.Y, next.Y, percentage, tension, bias));
        }
        internal static Vector2 CubicInterpolate(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float percentage)
        {
            return new Vector2(
                Cubic(a.X, b.X, c.X, d.X, percentage),
                Cubic(a.Y, b.Y, c.Y, d.Y, percentage));
        }
        internal static float Hermite(float v1, float v2, float v3, float v4, float aPercentage, float aTension, float aBias)
        {
            float mu2 = aPercentage * aPercentage;
            float mu3 = mu2 * aPercentage;
            float m0 = (v2 - v1) * (1 + aBias) * (1 - aTension) / 2;
            m0 += (v3 - v2) * (1 - aBias) * (1 - aTension) / 2;
            float m1 = (v3 - v2) * (1 + aBias) * (1 - aTension) / 2;
            m1 += (v4 - v3) * (1 - aBias) * (1 - aTension) / 2;
            float a0 = 2 * mu3 - 3 * mu2 + 1;
            float a1 = mu3 - 2 * mu2 + aPercentage;
            float a2 = mu3 - mu2;
            float a3 = -2 * mu3 + 3 * mu2;

            return (a0 * v2 + a1 * m0 + a2 * m1 + a3 * v3);
        }
        internal static float Cubic(float v1, float v2, float v3, float v4, float aPercentage)
        {
            float percentageSquared = aPercentage * aPercentage;
            float a1 = v4 - v3 - v1 + v2;
            float a2 = v1 - v2 - a1;
            float a3 = v3 - v1;
            float a4 = v2;

            return a1 * aPercentage * percentageSquared + a2 * percentageSquared + a3 * aPercentage + a4;
        }
        internal static void RenameKey<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey fromKey, TKey toKey)
        {
            TValue value = dic[fromKey];
            dic.Remove(fromKey);
            dic[toKey] = value;
        }
        internal static IEnumerable<TValue[]> BatchOfTakeUntil<TValue>(this IEnumerable<TValue> source, Predicate<TValue> predicate)
        {
            var batch = new List<TValue>();
            foreach (TValue value in source)
            {
                batch.Add(value);
                if (predicate(value))
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                yield return batch.ToArray();
        }
        internal static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }
        internal static Vector3 FindAnyPerpendicular(this Vector3 n)
        {
            n.Normalize();
            var vector3D = Vector3.Cross(Vector3.UnitY, n);
            if (vector3D.LengthSquared < 0.001)
                vector3D = Vector3.Cross(Vector3.UnitX, n);
            return vector3D;
        }
        internal static void Merge(ref BoundingBox box, Vector3 point)
        {
            if (point.X < box.Min.X)
                box.Min.X = point.X;
            if (point.Y < box.Min.Y)
                box.Min.Y = point.Y;
            if (point.Z < box.Min.Z)
                box.Min.Z = point.Z;
            if (point.X > box.Max.X)
                box.Max.X = point.X;
            if (point.Y > box.Max.Y)
                box.Max.Y = point.Y;
            if (point.Z > box.Max.Z)
                box.Max.Z = point.Z;
        }
        internal static Color ToUrhoColor(this System.Windows.Media.Color color)
        {
            return new Color(color.R/255f, color.G/255f, color.B/255f, color.A/255f);
        }

        public static T[] IntoANewArray<T>(this T head)
        {
            return new[] {head};
        }
        public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetCallingAssembly();

            if (pathInApplication[0] == Path.DirectorySeparatorChar)
                pathInApplication = pathInApplication.Substring(1);

            return new BitmapImage(new Uri($"pack://application:,,,/{assembly.GetName().Name};component/{pathInApplication}", UriKind.Absolute));
        }
        public static Vector3 ToVector3(this Vector2 vector, float z = 0)
        {
            return new Vector3(vector.X,vector.Y,z);
        }
        public static Vector2 ToVector(this Vector3 point, int decimalRounds = 2)
        {
            return new Vector2((float) Math.Round(point.X, decimalRounds),(float) Math.Round(point.Y,decimalRounds));
        }
        public static string GetRelativePath(string filespec)
        {
            Uri pathUri = new Uri(filespec);
            Uri folderUri = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        public static string RelativePath(this FileInfo info)
        {
            return GetRelativePath(info.FullName);
        }
        public static string RelativePath(this DirectoryInfo info)
        {
            return GetRelativePath(info.FullName);
        }
        public static bool PlaneIntersection(this Ray ray, Vector3 position, Vector3 normal, out Vector3 intersection)
        {
            float num1 = Vector3.Dot(normal, ray.Direction);
            if (num1 == 0.0f)
            {
                intersection = new Vector3(float.NaN, float.NaN, float.NaN);
                return false;
            }

            float num2 = Vector3.Dot(normal, position - ray.Origin) / num1;
            intersection = ray.Origin + num2 * ray.Direction;
            return true;
        }
        public static Vector3 ScreenPointToWorld(this Viewport vp, int x, int y, Plane plane)
        {
            var ray = vp.GetScreenRay(x, y);

            Vector3 intersection;
            ray.PlaneIntersection(plane.Normal * plane.D, plane.Normal, out intersection);
            return intersection;
        }
        public static Vector2 TopLeft(this Rect r)
        {
            return r.Min;
        }
        public static Vector2 TopRight(this Rect r)
        {
            return new Vector2(r.Min.X + r.Max.X, r.Min.Y);
        }
        public static Vector2 BottomLeft(this Rect r)
        {
            return new Vector2(r.Min.X, r.Min.Y +  r.Max.Y);
        }
        public static Vector2 BottomRight(this Rect r)
        {
            return new Vector2(r.Min.X + r.Max.X, r.Min.Y +  r.Max.Y);
        }

    }

}
