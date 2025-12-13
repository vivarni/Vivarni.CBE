using Microsoft.EntityFrameworkCore;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.EntityFrameworkCore.Setup;

namespace Vivarni.CBE.ConsoleSqlite;

internal class SearchDbContext : DbContext
{
    private readonly IDatabaseObjectNameProvider _objectNameProvider;

    public SearchDbContext(IDatabaseObjectNameProvider objectNameProvider)
    {
        _objectNameProvider = objectNameProvider;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=cbe_demo.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyDefaultCbeConfiguration(_objectNameProvider); // This applies the default confguration for the available Data Store.
    }
}
