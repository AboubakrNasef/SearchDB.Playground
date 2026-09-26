namespace Domain;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductSkuSnapshot { get; private set; }
    public string ProductNameSnapshot { get; private set; }
    public decimal UnitPriceSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPriceSnapshot * Quantity;

    private OrderItem()
    {
        ProductSkuSnapshot = ProductNameSnapshot = string.Empty;
    }

    public OrderItem(Guid id, Guid productId, string productSkuSnapshot, string productNameSnapshot, decimal unitPriceSnapshot, int quantity)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order item ID cannot be empty.", nameof(id));
        if (productId == Guid.Empty) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productSkuSnapshot)) throw new ArgumentException("Product SKU snapshot is required.", nameof(productSkuSnapshot));
        if (string.IsNullOrWhiteSpace(productNameSnapshot)) throw new ArgumentException("Product name snapshot is required.", nameof(productNameSnapshot));
        if (unitPriceSnapshot < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPriceSnapshot));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        Id = id;
        ProductId = productId;
        ProductSkuSnapshot = productSkuSnapshot.Trim();
        ProductNameSnapshot = productNameSnapshot.Trim();
        UnitPriceSnapshot = unitPriceSnapshot;
        Quantity = quantity;
    }
}
