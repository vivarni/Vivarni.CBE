namespace Vivarni.CBE.DataStorage;


/// <summary>
/// Defines methods for managing and retrieving the current extract number in the CBE state registry.
/// </summary>
public interface ICbeStateRegistry
{
    /// <summary>
    /// Gets the current extract number from the state registry.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The current CBE extract number.</returns>
    Task<int> GetCurrentExtractNumber(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the current extract number in the state registry.
    /// </summary>
    /// <param name="extractNumber">The extract number to set as current.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task SetCurrentExtractNumber(int extractNumber, CancellationToken cancellationToken);
}
