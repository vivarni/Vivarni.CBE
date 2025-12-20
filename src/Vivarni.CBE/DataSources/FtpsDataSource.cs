using System.Text;
using Renci.SshNet;
using Vivarni.CBE.DataSources.Security;

namespace Vivarni.CBE.DataSources
{
    internal class FtpsDataSource : ICbeDataSource, IDisposable
    {
        private const string ECONOMIE_FGOV_BE_URL = "ftps.economie.fgov.be";
        private readonly ICbeCredentialProvider _credentialProvider;

        private SftpClient? _client;
        private bool _disposed = false;

        public FtpsDataSource(ICbeCredentialProvider credentialProvider)
        {
            _credentialProvider = credentialProvider;
        }

        public async Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken = default)
        {
            var client = GetClient();
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
            var client = GetClient();
            var (username, _) = await _credentialProvider.GetCredentials(cancellationToken);
            return client.OpenRead($"/home/{username}/{file.Filename}");
        }

        private SftpClient GetClient()
        {
            if (_client == null)
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
                _client = new SftpClient(connectionInfo);
            }

            if (!_client.IsConnected)
                _client.Connect();

            return _client;
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
                _client?.Dispose();
                _disposed = true;
            }
        }
    }
}
