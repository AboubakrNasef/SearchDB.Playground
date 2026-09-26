using Domain;

namespace SearchDB.SeedData;

public static class DemoRecords
{
    public static IReadOnlyList<Product> Products { get; } =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "KEY-001", "Mechanical Keyboard", "Accessories", "Compact keyboard with tactile switches", 89.99m, "USD", true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "MSE-002", "Wireless Mouse", "Accessories", "Ergonomic wireless mouse", 39.50m, "USD", true),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "MON-003", "Studio Monitor", "Displays", "27 inch 4K display for creative work", 429.00m, "USD", true),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "CAB-004", "USB-C Cable", "Cables", "Durable braided charging cable", 12.00m, "USD", false)
    ];

    public static IReadOnlyList<Order> Orders { get; } =
    [
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "ORD-1001", DateTimeOffset.Parse("2026-01-12T09:30:00Z"), OrderStatus.Completed,
        [
            new(Guid.Parse("a1000000-0000-0000-0000-000000000001"), Guid.Parse("11111111-1111-1111-1111-111111111111"), "KEY-001", "Mechanical Keyboard", 89.99m, 1),
            new(Guid.Parse("a1000000-0000-0000-0000-000000000002"), Guid.Parse("22222222-2222-2222-2222-222222222222"), "MSE-002", "Wireless Mouse", 39.50m, 1)
        ]),
        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "ORD-1002", DateTimeOffset.Parse("2026-02-05T14:15:00Z"), OrderStatus.InProgress,
        [new(Guid.Parse("b2000000-0000-0000-0000-000000000001"), Guid.Parse("33333333-3333-3333-3333-333333333333"), "MON-003", "Studio Monitor", 429m, 1)]),
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "ORD-1003", DateTimeOffset.Parse("2026-03-18T11:00:00Z"), OrderStatus.Pending,
        [new(Guid.Parse("c3000000-0000-0000-0000-000000000001"), Guid.Parse("44444444-4444-4444-4444-444444444444"), "CAB-004", "USB-C Cable", 12m, 3)])
    ];
}
