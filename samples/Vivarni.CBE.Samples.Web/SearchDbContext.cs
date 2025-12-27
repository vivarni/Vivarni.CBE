using Microsoft.EntityFrameworkCore;

namespace Vivarni.CBE.Samples.ElasticSearch;


internal class SearchDbContext : DbContext
{
    public const string SCHEMA_NAME = "kbo";

    public SearchDbContext(DbContextOptions<SearchDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDbContext).Assembly);
    }
}
