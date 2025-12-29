using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.Samples.Common;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Web;

public interface IElasticRepository
{
    Task ReindexAllEnterprisesAsync(CancellationToken cancellationToken = default);
    Task ReindexEnterpriseAsync(string v);
    Task<List<Enterprise>> SearchAsync(string input);
}

public class ElasticRepository : IElasticRepository
{
    private const int PAGE_SIZE = 100_000;
    private static readonly string[] s_value = ["enterpriseNumber", "denominations", "addresses", "addresses.keyword"];

    private readonly ILogger _logger;
    private readonly SearchDbContext _context;
    private readonly ElasticsearchClient _elasticClient;

    public ElasticRepository(SearchDbContext context, ILogger<ElasticRepository> logger)
    {
        _context = context;
        _logger = logger;
        _elasticClient = new ElasticsearchClient(new Uri("http://localhost:9200"));
    }

    public async Task<List<Enterprise>> SearchAsync(string input)
    {
        // Perform a search query in Elasticsearch
        var searchResponse = await _elasticClient.SearchAsync<ElasticDoc>(s => s
            .Indices("cbe")
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(s_value)
                    .Query(input)
                )
            )
            .Size(50) // Limit to top 50 results
        );

        if (!searchResponse.IsValidResponse)
        {
            _logger.LogError("Search query failed: {Reason}", searchResponse.DebugInformation);
            return [];
        }

        // Extract EnterpriseNumbers from the search results
        var enterpriseNumbers = searchResponse.Hits.Select(hit => hit.Source.EnterpriseNumber).ToList();

        // Query the database to retrieve the full Enterprise entities
        var enterprises = await _context.Set<Enterprise>()
            .AsNoTracking()
            .AsSingleQuery()
            .Include(s => s.Addresses)
            .Include(s => s.Denominations)
            .Where(e => enterpriseNumbers.Contains(e.EnterpriseNumber))
            .ToListAsync();

        return enterprises;
    }

    public async Task ReindexAllEnterprisesAsync(CancellationToken cancellationToken = default)
    {
        await RecreateIndex(cancellationToken);
        var (query, total) = await GetDataQuery();
        var pageIndex = 0;
        double done = 0;

        while (true)
        {
            var page = query
                .Skip(pageIndex * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList()
                .Select(s => new ElasticDoc(s))
                .ToList();

            if (page.Count == 0)
                break;

            done += page.Count;
            var bulkResponse = await _elasticClient.BulkAsync(b => b
                    .Index("cbe")
                    .IndexMany(page));

            if (!bulkResponse.IsValidResponse)
                _logger.LogError("Bulk upload failed. Debug information: {debugInfo}", bulkResponse.DebugInformation);
            else
                _logger.LogInformation("Indexed {count:N0} documents. Progress: {progress:P2}", done, done / total);

            pageIndex++;
        }
    }

    private async Task<(IQueryable<Enterprise> query, double total)> GetDataQuery()
    {
        var query = _context.Set<Enterprise>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Addresses)
            .Include(s => s.Denominations)
            .OrderBy(e => e.EnterpriseNumber);

        var total = (double)_context.Set<Enterprise>().Count();
        _logger.LogInformation("Found {num:N0} enterprises to reindex...", total);

        return (query, total);
    }

    private async Task RecreateIndex(CancellationToken cancellationToken)
    {
        var createIndexRequest = new CreateIndexRequest("cbe")
        {
            Settings = new IndexSettings
            {
                NumberOfReplicas = 0,
                NumberOfShards = 1
            },
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { "EnterpriseNumber", new KeywordProperty() }
                }
            }
        };

        _logger.LogInformation("Recreating index...");
        await _elasticClient.Indices.DeleteAsync("cbe", cancellationToken);

        var indexResponse = await _elasticClient.Indices.CreateAsync(createIndexRequest, cancellationToken);
        if (!indexResponse.IsValidResponse)
        {
            throw new Exception("Failed to create index: " + indexResponse.ElasticsearchServerError?.ToString());
        }
    }

    public async Task ReindexEnterpriseAsync(string enterpriseNumber)
    {
        var (query, total) = await GetDataQuery();

        var enterprise = query.SingleOrDefault(s => s.EnterpriseNumber == enterpriseNumber);
        if (enterprise == null)
            return;

        var doc = new ElasticDoc(enterprise);
        _elasticClient.Index(doc);
    }
}
