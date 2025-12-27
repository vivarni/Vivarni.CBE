using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.ElasticSearch.DomainModels;

namespace Vivarni.CBE.Samples.ElasticSearch.DatabaseMappings;

internal class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("cbe_contact", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("cbe_id");
        builder.Property(e => e.EntityNumber).HasColumnName("entity_number");
        builder.Property(e => e.EntityContact).HasColumnName("entity_contact");
        builder.Property(e => e.ContactType).HasColumnName("contact_type");
        builder.Property(e => e.Value).HasColumnName("value");
    }
}
