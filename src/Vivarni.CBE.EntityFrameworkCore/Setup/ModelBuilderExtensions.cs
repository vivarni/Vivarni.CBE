using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataSources.Entities;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.EntityFrameworkCore.Setup;

public static class ModelBuilderExtensions
{
    public static void ApplyDefaultCbeConfiguration(this ModelBuilder modelBuilder, IDatabaseObjectNameProvider nameProvider)
    {
        modelBuilder.ApplyDefaultCbeConfiguration<CbeActivity>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeAddress>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeBranch>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeCode>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeContact>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeDenomination>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeEnterprise>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeEstablishment>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeMeta>(nameProvider);
    }

    private static void ApplyDefaultCbeConfiguration<T>(this ModelBuilder modelBuilder, IDatabaseObjectNameProvider nameProvider)
        where T : class, ICbeEntity
    {
        var tableName = nameProvider.GetTableName<T>();

        modelBuilder
            .Entity<T>()
            .ToTable(tableName)
            .HasDefaultCbeKey();
    }

    private static EntityTypeBuilder<T> HasDefaultCbeKey<T>(this EntityTypeBuilder<T> entityTypeBuilder)
        where T : class, ICbeEntity
    {
        dynamic builder = entityTypeBuilder;
        builder.HasDefaultCbeKey();
        return entityTypeBuilder;
    }
}
