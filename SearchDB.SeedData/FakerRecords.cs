using Bogus;
using Domain;
using System.Globalization;

namespace SearchDB.SeedData;

public static class FakerRecords
{
    public static (IReadOnlyList<Product> Products, IReadOnlyList<Order> Orders) Generate(int count =100)
    {
        var faker = new Faker("en") { Random = new Randomizer(1337) };
        var products = Enumerable.Range(1, count/10).Select(i =>
        {
            var name = faker.Commerce.ProductName();
            return new Product(Id(i), $"FAKE-{i:0000}", name, faker.Commerce.Department(),
                faker.Commerce.ProductDescription(), decimal.Parse(faker.Commerce.Price(5, 1500), CultureInfo.InvariantCulture), "USD", faker.Random.Bool(0.9f));
        }).ToArray();

        var orders = Enumerable.Range(1, count).Select(i =>
        {
            var items = faker.Random.ArrayElements(products, faker.Random.Int(1, 4)).Select((product, itemIndex) =>
                new OrderItem(Id(10_000 + i * 4 + itemIndex), product.Id, product.Sku, product.Name, product.UnitPrice, faker.Random.Int(1, 5))).ToArray();
            return new Order(Id(1000 + i), $"FAKE-ORD-{i:00000}", faker.Date.RecentOffset(730),
                faker.Random.Enum<OrderStatus>(), items);
        }).ToArray();

        return (products, orders);
    }

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
}
