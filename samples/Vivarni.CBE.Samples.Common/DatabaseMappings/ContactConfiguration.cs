using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

internal class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("cbe_contact", SearchDbContext.SCHEMA_NAME);

        // Shadow Properties and Primary Key
        builder.Property<long>("cbe_id");
        builder.Property<string>("entity_number");
        builder.HasKey("cbe_id");

        builder.Property(e => e.EntityContact).HasColumnName("entity_contact");
        builder.Property(e => e.ContactType).HasColumnName("contact_type");
        builder.Property(e => e.Value).HasColumnName("value");
    }
}
