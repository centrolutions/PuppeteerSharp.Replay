using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PuppeteerSharp.Replay
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] item)
        {
            return source.Concat(item);
        }
    }
}
