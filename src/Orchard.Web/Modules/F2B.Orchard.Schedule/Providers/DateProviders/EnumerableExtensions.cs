using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Drivers;

namespace F2B.Orchard.Schedule.Providers
{
    class EnumeratorComparer<T> : IComparer<IEnumerator<T>> where T : IComparable<T> {
        public int Compare(IEnumerator<T> x, IEnumerator<T> y) {
            return x.Current.CompareTo(y.Current);
        }
    }
    
    public static class EnumerableExtensions
    {
        // This is very similar to SelectMany except that it doesn't try to enumerate all the sources at once, but returns the results from the enumerators in order
        // It is assumed that the enumerables returned by selector are already ordered, which is the case for any of the date providers.

        public static IEnumerable<TResult> MergeOrdered<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector) where TResult : IComparable<TResult>
        {
            var enumerators =
                source
                .Select(s => selector(s).GetEnumerator())
                .Where(e => e.MoveNext())
                .ToList();
                //.ToDictionary(e => e, e => true);

            //var enumeratorSet = new SortedSet<IEnumerator<TResult>>(enumerators, new EnumeratorComparer<TResult>()); 

            while (enumerators.Any())
            {
                var smallest = enumerators.OrderBy(e => e.Current).First();
                yield return smallest.Current;
                if (!smallest.MoveNext()) enumerators.Remove(smallest);
                //enumerators[smallest.Key] = smallest.Key.MoveNext();
                //var smallest = enumeratorSet.First();
                //enumeratorSet.Remove(smallest);
                //yield return smallest.Current;
                //if (smallest.MoveNext()) enumeratorSet.Add(smallest);
            }
        }

        public static IEnumerable<TResult> IntersectOrdered<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> selector) where TResult : IEquatable<TResult>
        {
            List<IEnumerator<TResult>> enumerators = source.Select(s => selector(s).GetEnumerator()).ToList();
            // initialize enumators

            bool done = enumerators.Any(e => !e.MoveNext());

            while (!done)
            {
                var smallest = enumerators.OrderBy(e => e.Current).First();
                if (enumerators.All(e => e.Current.Equals(smallest.Current))) yield return smallest.Current;
                done = !smallest.MoveNext();
            }
        }

        public static IEnumerable<T> DifferenceOrdered<T>(this IEnumerable<T> include, IEnumerable<T> exclude, IComparer<T> comparer) {
            var includeEnumerator = include.GetEnumerator();
            var excludeEnumerator = exclude.GetEnumerator();

            if (!includeEnumerator.MoveNext()) yield break; // if nothing in the source, there is nothing

            if (excludeEnumerator.MoveNext())
            {
                while (true)
                {
                    var compare = comparer.Compare(includeEnumerator.Current, excludeEnumerator.Current);
                    if (compare < 0)
                    {
                        yield return includeEnumerator.Current;
                        if (!includeEnumerator.MoveNext()) yield break;
                    }
                    else if (compare > 0)
                    {
                        if (!excludeEnumerator.MoveNext()) break;
                    }
                    else
                    {
                        if (!includeEnumerator.MoveNext()) yield break;
                        if (!excludeEnumerator.MoveNext()) break;
                    }
                }
            }

            do
            {
                yield return includeEnumerator.Current;
            } while (includeEnumerator.MoveNext());
        }

        public static IEnumerable<T> DifferenceOrdered<T>(this IEnumerable<T> include, IEnumerable<T> exclude) where T : IComparable<T> {
            return include.DifferenceOrdered(exclude, Comparer<T>.Create((a,b)=>a.CompareTo(b)));
        }

        public static IEnumerable<DateTime> Until(this IEnumerable<DateTime> source, DateTime end)
        {
            return source.TakeWhile(d => d <= end);
        }
    }
}