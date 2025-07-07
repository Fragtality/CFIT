using System;
using System.Collections.Generic;
using System.Linq;

namespace CFIT.AppTools
{
    public static partial class Enumerable
    {
        public static TSource SafeLast<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null || source?.Count() == 0)
                return (TSource)(object)null;

            return source.Last();
        }

        public static TSource SafeLast<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null || source?.Count() == 0)
                return (TSource)(object)null;

            return source.Last(predicate);
        }
    }
}
