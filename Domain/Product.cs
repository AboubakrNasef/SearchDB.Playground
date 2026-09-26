namespace Domain;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; }
    public bool IsActive { get; private set; }

    private Product()
    {
        Sku = Name = Category = Description = Currency = string.Empty;
    }

    public Product(Guid id, string sku, string name, string category, string description, decimal unitPrice, string currency, bool isActive)
    {
        if (id == Guid.Empty) throw new ArgumentException("Product ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (currency is null || currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter uppercase code.", nameof(currency));

        Id = id;
        Sku = sku.Trim();
        Name = name.Trim();
        Category = category.Trim();
        Description = description ?? string.Empty;
        UnitPrice = unitPrice;
        Currency = currency;
        IsActive = isActive;
    }
}
