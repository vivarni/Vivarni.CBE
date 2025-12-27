using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.ElasticSearch.DomainModels;

namespace Vivarni.CBE.Samples.ElasticSearch.DatabaseMappings;

internal class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("cbe_address", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("cbe_id");
        builder.Property(e => e.EntityNumber).HasColumnName("entity_number");
        builder.Property(e => e.TypeOfAddress).HasColumnName("type_of_address");
        builder.Property(e => e.CountryNL).HasColumnName("country_nl");
        builder.Property(e => e.CountryFR).HasColumnName("country_fr");
        builder.Property(e => e.Zipcode).HasColumnName("zipcode");
        builder.Property(e => e.MunicipalityNL).HasColumnName("municipality_nl");
        builder.Property(e => e.MunicipalityFR).HasColumnName("municipality_fr");
    }
}
