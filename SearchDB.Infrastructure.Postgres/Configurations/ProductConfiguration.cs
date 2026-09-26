using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace SearchDB.Infrastructure.Postgres.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(80).IsRequired();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(p => p.UnitPrice).HasColumnName("unit_price").HasPrecision(12, 2);
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active");
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(sku, '') || ' ' || coalesce(name, '') || ' ' || coalesce(category, '') || ' ' || coalesce(description, ''))", stored: true);
        builder.HasIndex("SearchVector").HasMethod("GIN");
    }
}
