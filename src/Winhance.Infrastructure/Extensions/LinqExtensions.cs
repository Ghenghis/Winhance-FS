using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Winhance.Infrastructure.Extensions
{
    /// <summary>
    /// Extensions for LINQ operations
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Converts IEnumerable to IAsyncEnumerable for compatibility
        /// </summary>
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield(); // Allow other tasks to run
            }
        }

        /// <summary>
        /// Asynchronously iterates over an async enumerable and executes an action for each item
        /// </summary>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> source, Func<T, Task> action, CancellationToken cancellationToken = default)
        {
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                await action(item);
            }
        }

        /// <summary>
        /// Asynchronously converts an async enumerable to a list
        /// </summary>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            var list = new List<T>();
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                list.Add(item);
            }
            return list;
        }
    }
}
