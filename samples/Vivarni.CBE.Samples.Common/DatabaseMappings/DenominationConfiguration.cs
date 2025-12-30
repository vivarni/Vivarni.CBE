using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

internal class DenominationConfiguration : IEntityTypeConfiguration<Denomination>
{
    public void Configure(EntityTypeBuilder<Denomination> builder)
    {
        builder.ToTable("cbe_denomination", SearchDbContext.SCHEMA_NAME);

        // Shadow Properties and Primary Key
        builder.Property<long>("cbe_id");
        builder.Property<string>("entity_number");
        builder.HasKey("cbe_id");

        builder.Property(e => e.Language).HasColumnName("language");
        builder.Property(e => e.TypeOfDenomination).HasColumnName("type_of_denomination");
        builder.Property(e => e.Name).HasColumnName("denomination");
    }
}
