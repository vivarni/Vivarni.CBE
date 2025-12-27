using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DatabaseMappings;

internal class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("cbe_activity", SearchDbContext.SCHEMA_NAME);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("cbe_id");
        builder.Property(e => e.NaceCode).HasColumnName("nace_code");
        builder.Property(e => e.NaceVersion).HasColumnName("nace_version");
        builder.Property(e => e.ActivityGroup).HasColumnName("activity_group");
        builder.Property(e => e.EntityNumber).HasColumnName("entity_number");
        builder.Property(e => e.Classification).HasColumnName("classification");
    }
}
