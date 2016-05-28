using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistDotNet.Persist
{
    internal static class Utils
    {
        public static Type GetEnumeratedType(this Type type)
        {
            // provided by Array
            var theType = type.GetElementType();
            if (null != theType) return theType;

            // otherwise provided by collection
            var theTypes = type.GetGenericArguments();
            if (theTypes.Length > 0) return theTypes[0];

            // otherwise is not an 'enumerated' type
            return null;
        }

        public static bool HasCircularDependency<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
        {
            return source.Any(item => Visit(item, getDependencies, new Dictionary<T, bool>()));
        }
        private static bool Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, IDictionary<T, bool> visited)
        {
            bool inProcess;
            var alreadyVisited = visited.TryGetValue(item, out inProcess);

            if (alreadyVisited)
            {
                if (inProcess) return true;
            }
            else
            {
                visited[item] = true;

                if (getDependencies(item).Any(dependency => Visit(dependency, getDependencies, visited)))
                    return true;

                visited[item] = false;
            }

            return false;
        }

        public static bool AllSame<T,TOut>(this IEnumerable<T> enumerable, Func<T, TOut> selector,bool returnValueIfEmpty = false)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            var enumerator = enumerable.GetEnumerator();

            TOut toCompare;
            if (enumerator.MoveNext())
                toCompare = selector(enumerator.Current);
            else
                return returnValueIfEmpty;

            while (enumerator.MoveNext())
            {
                if (!toCompare.Equals(selector(enumerator.Current)))
                    return false;
            }

            return true;
        }

        public static int CountSame<T, TOut>(this IEnumerable<T> enumerable, Func<T, TOut> selector)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            var enumerator = enumerable.GetEnumerator();

            TOut toCompare;
            if (enumerator.MoveNext())
                toCompare = selector(enumerator.Current);
            else
                return 0;

            var count = 1;

            while (enumerator.MoveNext())
            {
                if (toCompare.Equals(selector(enumerator.Current)))
                    count++;
            }

            return  count == 1 ? 0 : count;
        }

    }
}