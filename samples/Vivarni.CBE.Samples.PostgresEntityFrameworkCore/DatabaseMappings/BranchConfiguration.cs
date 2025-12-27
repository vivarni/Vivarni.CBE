using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DatabaseMappings;

internal class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("cbe_branch", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.StartDate).HasColumnName("start_date");
        builder.Property(e => e.EnterpriseNumber).HasColumnName("enterprise_number");

        builder.HasMany(e => e.Denominations)
            .WithOne()
            .HasForeignKey(d => d.EntityNumber);

        builder.HasMany(e => e.Addresses)
            .WithOne()
            .HasForeignKey(e => e.EntityNumber);
    }
}
