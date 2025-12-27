using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DatabaseMappings;

internal class EnterpriseConfiguration : IEntityTypeConfiguration<Enterprise>
{
    public void Configure(EntityTypeBuilder<Enterprise> builder)
    {
        builder.ToTable("cbe_enterprise", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.EnterpriseNumber);

        builder.Property(e => e.EnterpriseNumber).HasColumnName("enterprise_number");
        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.StartDate).HasColumnName("start_date");
        builder.Property(e => e.JuridicalSituation).HasColumnName("juridical_situation");
        builder.Property(e => e.TypeOfEnterprise).HasColumnName("type_of_enterprise");
        builder.Property(e => e.JuridicalForm).HasColumnName("juridical_form");
        builder.Property(e => e.JuridicalFormCAC).HasColumnName("juridical_form_cac");

        builder.HasMany(e => e.Denominations)
            .WithOne()
            .HasForeignKey(e => e.EntityNumber);

        builder.HasMany(e => e.Contacts)
            .WithOne()
            .HasForeignKey(e => e.EntityNumber);

        builder.HasMany(e => e.Branches)
            .WithOne()
            .HasForeignKey(e => e.EnterpriseNumber);

        builder.HasMany(e => e.Establishments)
            .WithOne()
            .HasForeignKey(e => e.EnterpriseNumber);

        builder.HasMany(e => e.Activities)
            .WithOne()
            .HasForeignKey(e => e.EntityNumber);

        builder.HasMany(e => e.Addresses)
            .WithOne()
            .HasForeignKey(e => e.EntityNumber);
    }
}
