using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

internal class CodeTranslationConfiguration : IEntityTypeConfiguration<CodeTranslation>
{
    public void Configure(EntityTypeBuilder<CodeTranslation> builder)
    {
        builder.ToTable("cbe_code", SearchDbContext.SCHEMA_NAME);
        builder.Property<long>("cbe_id");
        builder.HasKey("cbe_id");

        builder.Property(e => e.Category).HasColumnName("category");
        builder.Property(e => e.Code).HasColumnName("code");
        builder.Property(e => e.Language).HasColumnName("language");
        builder.Property(e => e.Description).HasColumnName("description");
    }
}
