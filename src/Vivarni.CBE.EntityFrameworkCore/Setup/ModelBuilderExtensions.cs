using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Entities;

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
        modelBuilder
            .ApplyDefaultCbeConfiguration<CbeEnterprise>(nameProvider)
            .HasMany(e => e.Denominations).WithOne();

        modelBuilder.ApplyDefaultCbeConfiguration<CbeEstablishment>(nameProvider);
        modelBuilder.ApplyDefaultCbeConfiguration<CbeMeta>(nameProvider);
    }

    private static EntityTypeBuilder<T> ApplyDefaultCbeConfiguration<T>(this ModelBuilder modelBuilder, IDatabaseObjectNameProvider nameProvider)
        where T : class, ICbeEntity
    {
        var tableName = nameProvider.GetTableName<T>();

        return modelBuilder
            .Entity<T>()
            .ToTable(tableName)
            .HasDefaultCbeKey();
    }

    private static EntityTypeBuilder<T> HasDefaultCbeKey<T>(this EntityTypeBuilder<T> entityTypeBuilder)
        where T : class, ICbeEntity
    {
        var primaryKeyAttribute = typeof(T).GetCustomAttribute<CbePrimaryKeyAttribute>();
        if (primaryKeyAttribute != null && primaryKeyAttribute.PropertyNames.Count > 0)
        {
            entityTypeBuilder.HasKey([.. primaryKeyAttribute.PropertyNames]);
        }
        else
        {
            entityTypeBuilder.HasNoKey();
        }

        return entityTypeBuilder;
    }
}
