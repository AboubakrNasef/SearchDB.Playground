using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace SearchDB.Infrastructure.Postgres.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.ProductSkuSnapshot).HasColumnName("product_sku_snapshot").HasMaxLength(80).IsRequired();
        builder.Property(i => i.ProductNameSnapshot).HasColumnName("product_name_snapshot").HasMaxLength(200).IsRequired();
        builder.Property(i => i.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(12, 2);
        builder.Property(i => i.Quantity).HasColumnName("quantity");
        builder.Ignore(i => i.LineTotal);
        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(product_sku_snapshot, '') || ' ' || coalesce(product_name_snapshot, ''))", stored: true);
        builder.HasIndex("SearchVector").HasMethod("GIN");
    }
}
