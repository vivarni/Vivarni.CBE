namespace Vivarni.CBE.DataSources.Security;

public class SimpleCredentialProvider : ICbeCredentialProvider
{
    private readonly string _username;
    private readonly byte[] _password;

    public SimpleCredentialProvider(string username, byte[] password)
    {
        _username = username;
        _password = password;
    }

    public Task<(string username, byte[] passwordUtf8)> GetCredentials(CancellationToken cancellationToken)
    {
        return Task.FromResult((_username, _password));
    }
}
