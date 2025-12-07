using System.Net;
using System.Text;
using Vivarni.CBE.DataSources.Security;

namespace Vivarni.CBE.DataSources;

internal class HttpCbeDataSource : ICbeDataSource
{
    private const string LOGIN_URL = "https://kbopub.economie.fgov.be/kbo-open-data/static/j_spring_security_check";
    private const string INDEX_URL = "https://kbopub.economie.fgov.be/kbo-open-data/affiliation/xml/?files";

    private readonly ICbeCredentialProvider _credentialProvider;
    private readonly HttpClient _httpClient;

    private bool _isLoggedIn;

    public HttpCbeDataSource(ICbeCredentialProvider credentialProvider)
    {
        _isLoggedIn = false;
        _credentialProvider = credentialProvider;

        _httpClient = new HttpClient(new HttpClientHandler()
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = true
        });
    }

    public async Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken)
    {
        await EnsureLogin(cancellationToken);

        var indexResponse = (await _httpClient.GetAsync(INDEX_URL, cancellationToken)).EnsureSuccessStatusCode();
        var html = await indexResponse.Content.ReadAsStringAsync(cancellationToken);

        // Extract href attributes from anchor tags using Regex (no external libs)
        // Matches: <a ... href="value" ...> or single-quoted
        var hrefRegex = new System.Text.RegularExpressions.Regex(
            "<a[^>]*href\\s*=\\s*\"(files/.+\\.zip)\"|<a[^>]*href\\s*=\\s*'(files/.+\\.zip)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        var matches = hrefRegex.Matches(html);
        var hrefs = matches
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : (m.Groups[2].Success ? m.Groups[2].Value : null))
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        var result = hrefs
            .Select(s => new CbeOpenDataFile("https://kbopub.economie.fgov.be/kbo-open-data/affiliation/xml/" + s))
            .ToList();

        return result;
    }

    public async Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken)
    {
        await EnsureLogin(cancellationToken);
        return await _httpClient.GetStreamAsync(file.Source, cancellationToken);
    }

    private async Task EnsureLogin(CancellationToken cancellationToken)
    {
        if (_isLoggedIn)
            return;

        var (username, password) = await _credentialProvider.GetCredentials(cancellationToken);
        if (username == null || password == null)
            throw new Exception("Cannot proceed because the username and/or password is empty!");

        var formContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("j_username", username),
            new KeyValuePair<string, string>("j_password", Encoding.UTF8.GetString(password))
        ]);

        var loginResponse = await _httpClient.PostAsync(LOGIN_URL, formContent, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();
        _isLoggedIn = true;
    }
}
