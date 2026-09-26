using Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SearchDB.Infrastructure.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class OrderDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;
    [BsonElement("searchText")]
    public string SearchText { get; set; } = string.Empty;
    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }
    [BsonElement("items")]
    public List<OrderItemDocument> Items { get; set; } = [];

    public static OrderDocument FromDomain(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CreatedAt = order.CreatedAt.UtcDateTime,
        Status = order.Status.ToString(),
        SearchText = order.SearchText,
        TotalAmount = order.Items.Sum(item => item.LineTotal),
        Items = order.Items.Select(OrderItemDocument.FromDomain).ToList()
    };
}

[BsonIgnoreExtraElements]
public sealed class OrderItemDocument
{
    [BsonElement("productId")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProductId { get; set; }
    [BsonElement("productSkuSnapshot")]
    public string ProductSkuSnapshot { get; set; } = string.Empty;
    [BsonElement("productNameSnapshot")]
    public string ProductNameSnapshot { get; set; } = string.Empty;
    [BsonElement("unitPriceSnapshot")]
    public decimal UnitPriceSnapshot { get; set; }
    [BsonElement("quantity")]
    public int Quantity { get; set; }

    public static OrderItemDocument FromDomain(OrderItem item) => new()
    {
        ProductId = item.ProductId,
        ProductSkuSnapshot = item.ProductSkuSnapshot,
        ProductNameSnapshot = item.ProductNameSnapshot,
        UnitPriceSnapshot = item.UnitPriceSnapshot,
        Quantity = item.Quantity
    };
}

public static class MongoSearchCollections
{
    public const string Products = "products";
    public const string Orders = "orders";
}
