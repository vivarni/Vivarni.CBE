using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

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
        builder.Property(e => e.StreetNL).HasColumnName("street_nl");
        builder.Property(e => e.StreetFR).HasColumnName("street_fr");
        builder.Property(e => e.HouseNumber).HasColumnName("house_number");
        builder.Property(e => e.MunicipalityNL).HasColumnName("municipality_nl");
        builder.Property(e => e.MunicipalityFR).HasColumnName("municipality_fr");
        builder.Property(e => e.DateStrikingOff).HasColumnName("date_striking_off");
        builder.Property(e => e.ExtraAddressInfo).HasColumnName("extra_address_info");
        builder.Property(e => e.Zipcode).HasColumnName("zipcode");
        builder.Property(e => e.Box).HasColumnName("box");
    }
}
