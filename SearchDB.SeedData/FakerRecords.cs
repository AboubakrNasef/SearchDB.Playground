using Bogus;
using Domain;

namespace SearchDB.SeedData;

public static class FakerRecords
{
    public static IReadOnlyList<Order> GenerateOrders(int count = 1000)
    {
        var faker = new Faker("en");
        var products = DemoRecords.Products.ToArray();
        return Enumerable.Range(0, count).Select(_ =>
        {
            var items = faker.Random.ArrayElements(products, faker.Random.Int(1, 4)).Select((product, itemIndex) =>
                new OrderItem(Guid.NewGuid(), product.Id, product.Sku, product.Name, product.UnitPrice, faker.Random.Int(1, 5))).ToArray();
            var id = Guid.NewGuid();
            return new Order(id, $"FAKE-ORD-{id:N}", faker.Date.RecentOffset(730).ToUniversalTime(),
                faker.Random.Enum<OrderStatus>(), items);
        }).ToArray();
    }
}
