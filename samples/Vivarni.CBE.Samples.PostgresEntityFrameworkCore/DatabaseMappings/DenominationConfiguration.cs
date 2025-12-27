using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DatabaseMappings;

internal class DenominationConfiguration : IEntityTypeConfiguration<Denomination>
{
    public void Configure(EntityTypeBuilder<Denomination> builder)
    {
        builder.ToTable("cbe_denomination", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("cbe_id");
        builder.Property(e => e.EntityNumber).HasColumnName("entity_number");
        builder.Property(e => e.Language).HasColumnName("language");
        builder.Property(e => e.TypeOfDenomination).HasColumnName("type_of_denomination");
        builder.Property(e => e.Name).HasColumnName("denomination");
    }
}
