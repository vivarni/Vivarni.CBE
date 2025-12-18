using System.Text;
using Renci.SshNet;
using Vivarni.CBE.DataSources.Security;

namespace Vivarni.CBE.DataSources
{
    internal class FtpsDataSource : ICbeDataSource, IDisposable
    {
        private readonly ICbeCredentialProvider _credentialProvider;
        private readonly Lazy<SftpClient> _lazyClient;
        private const string ECONOMIE_FGOV_BE_URL = "ftps.economie.fgov.be";
        private bool _disposed = false;

        public FtpsDataSource(ICbeCredentialProvider credentialProvider)
        {
            _credentialProvider = credentialProvider;
            _lazyClient = new Lazy<SftpClient>(CreateClientSync);
        }

        public async Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken = default)
        {
            var client = _lazyClient.Value;
            var (username, _) = await _credentialProvider.GetCredentials(cancellationToken);

            var files = client
                .ListDirectory($"/home/{username}/")
                .ToList();

            var result = files
                .Where(s => s.Name.EndsWith(".zip"))
                .Select(s => new CbeOpenDataFile(s.Name))
                .ToList();

            return [.. result];
        }

        public async Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken = default)
        {
            var client = _lazyClient.Value;
            var (username, _) = await _credentialProvider.GetCredentials(cancellationToken);
            return client.OpenRead($"/home/{username}/{file.Filename}");
        }

        private SftpClient CreateClientSync()
        {
            var (username, passwordUtf8) = _credentialProvider.GetCredentials().GetAwaiter().GetResult();
            var password = Encoding.UTF8.GetString(passwordUtf8);

            // Create connection info with keyboard-interactive authentication
            var keyboardInteractiveAuth = new KeyboardInteractiveAuthenticationMethod(username);
            keyboardInteractiveAuth.AuthenticationPrompt += (sender, e) =>
            {
                foreach (var prompt in e.Prompts)
                {
                    if (prompt.Request.Contains("Password:", StringComparison.OrdinalIgnoreCase) ||
                        prompt.Request.Contains("password", StringComparison.OrdinalIgnoreCase))
                    {
                        prompt.Response = password;
                    }
                }
            };

            var connectionInfo = new ConnectionInfo(ECONOMIE_FGOV_BE_URL, 22, username, keyboardInteractiveAuth);
            var client = new SftpClient(connectionInfo);

            client.Connect();
            return client;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_lazyClient.IsValueCreated)
                {
                    _lazyClient.Value?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
