namespace Vivarni.CBE.DataSources.Security;

/// <summary>
/// Interface for providing credentials (username and password) for CBE data sources.
/// </summary>
public interface ICbeCredentialProvider
{
    /// <summary>
    /// Asynchronously retrieves credentials for authentication.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple containing the username and password (as UTF-8 bytes).</returns>
    Task<(string username, byte[] passwordUtf8)> GetCredentials(CancellationToken cancellationToken = default);
}
