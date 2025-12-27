using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vivarni.CBE.Entities;

namespace Vivarni.CBE.EntityFrameworkCore.Setup;

public static class EntityTypeBuilderExtensions
{
    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeActivity> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => new { e.EntityNumber, e.NaceCode, e.NaceVersion, e.Classification, e.ActivityGroup });

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeAddress> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => new { e.EntityNumber, e.TypeOfAddress });

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeBranch> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => e.Id);

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeCode> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => new { e.Code, e.Language, e.Category });

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeContact> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => new { e.EntityNumber, e.Value, e.EntityContact, e.ContactType });

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeDenomination> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => new { e.EntityNumber, e.TypeOfDenomination, e.Language });

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeEnterprise> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => e.EnterpriseNumber);

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeEstablishment> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => e.EstablishmentNumber);

    public static KeyBuilder HasDefaultCbeKey(this EntityTypeBuilder<CbeMeta> entityTypeBuilder)
        => entityTypeBuilder.HasKey(e => e.Variable);
}
