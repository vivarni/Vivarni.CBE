namespace Vivarni.CBE.DataSources.Security;

public interface ICbeCredentialProvider
{
    Task<(string username, byte[] passwordUtf8)> GetCredentials(CancellationToken cancellationToken);
}
