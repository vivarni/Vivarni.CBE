using System.Collections;

namespace Vivarni.CBE.Util;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/> sequences.
/// </summary>
public static class IEnumerableExtensions
{
    /// <summary>
    /// Splits the source sequence into consecutive batches (chunks) of the given size.
    /// Each batch is yielded as an <see cref="IEnumerable{T}"/>. The final batch may be
    /// smaller than <paramref name="batchSize"/> if there are not enough elements.
    /// </summary>
    /// <typeparam name="T">The element type of the source sequence.</typeparam>
    /// <param name="source">The sequence to split into batches.</param>
    /// <param name="batchSize">The maximum size of each batch. Must be greater than zero.</param>
    /// <returns>An <see cref="IEnumerable"/> of IEnumerable's, where each inner sequence is a batch.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is less than 1.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (batchSize < 1)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "batchSize must be greater than zero.");

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return YieldBatchElements(enumerator, batchSize - 1);
        }
    }

    /// <summary>
    /// Yields the current element from the enumerator and up to <paramref name="batchSize"/>
    /// additional elements. Intended as a helper for <see cref="Batch{T}(IEnumerable{T},int)"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the enumerator.</typeparam>
    /// <param name="source">An enumerator positioned at the first element of the batch.</param>
    /// <param name="batchSize">The number of additional elements to yield after the current one.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> producing the elements in the batch.</returns>
    private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
    {
        yield return source.Current;
        for (var i = 0; i < batchSize && source.MoveNext(); i++)
        {
            yield return source.Current;
        }
    }
}
