namespace Vivarni.CBE.DataSources.Security;

public class SimpleCredentialProvider : ICbeCredentialProvider
{
    private readonly string _username;
    private readonly byte[] _password;

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    public SimpleCredentialProvider(string username, byte[] password)
    {
        _username = username;
        _password = password;
    }

    /// <inheritdoc/>
    public Task<(string username, byte[] passwordUtf8)> GetCredentials(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((_username, _password));
    }
}
