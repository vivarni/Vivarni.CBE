using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

internal class EstablishmentConfiguration : IEntityTypeConfiguration<Establishment>
{
    public void Configure(EntityTypeBuilder<Establishment> builder)
    {
        builder.ToTable("cbe_establishment", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.EstablishmentNumber);

        builder.Property(e => e.EstablishmentNumber).HasColumnName("establishment_number");
        builder.Property(e => e.StartDate).HasColumnName("start_date");
        builder.Property(e => e.EnterpriseNumber).HasColumnName("enterprise_number");
        builder.HasMany(e => e.Contacts)
            .WithOne()
            .HasForeignKey("entity_number");

        builder.HasMany(e => e.Activities)
            .WithOne()
            .HasForeignKey("entity_number");

        builder.HasMany(e => e.Denominations)
            .WithOne()
            .HasForeignKey("entity_number");

        builder.HasMany(e => e.Addresses)
            .WithOne()
            .HasForeignKey("entity_number");
    }
}
