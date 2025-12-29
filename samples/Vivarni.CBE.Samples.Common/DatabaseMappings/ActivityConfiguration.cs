using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common.DatabaseMappings;

internal class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("cbe_activity", SearchDbContext.SCHEMA_NAME);
        builder.Property<long>("cbe_id");
        builder.HasKey("cbe_id");

        builder.Property(e => e.NaceCode).HasColumnName("nace_code");
        builder.Property(e => e.NaceVersion).HasColumnName("nace_version");
        builder.Property(e => e.ActivityGroup).HasColumnName("activity_group");
        builder.Property(e => e.Classification).HasColumnName("classification");
    }
}
