namespace Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items;

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public string SearchText { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
        OrderNumber = string.Empty;
        SearchText = string.Empty;
        _items = [];
    }

    public Order(Guid id, string orderNumber, DateTimeOffset createdAt, OrderStatus status, IEnumerable<OrderItem> items)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Order number is required.", nameof(orderNumber));

        var orderItems = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        if (orderItems.Count == 0) throw new ArgumentException("An order must contain at least one item.", nameof(items));

        Id = id;
        OrderNumber = orderNumber.Trim();
        CreatedAt = createdAt;
        Status = status;
        _items = orderItems;
        SearchText = $"{OrderNumber} {Status} {string.Join(' ', orderItems.Select(item => $"{item.ProductSkuSnapshot} {item.ProductNameSnapshot}"))}";
    }
}
