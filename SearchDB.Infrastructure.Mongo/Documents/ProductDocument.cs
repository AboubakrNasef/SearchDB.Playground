using Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SearchDB.Infrastructure.Mongo.Documents;

[BsonIgnoreExtraElements]
public sealed class ProductDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }
    [BsonElement("currency")]
    public string Currency { get; set; } = string.Empty;
    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    public static ProductDocument FromDomain(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        Category = product.Category,
        Description = product.Description,
        UnitPrice = product.UnitPrice,
        Currency = product.Currency,
        IsActive = product.IsActive
    };
}
