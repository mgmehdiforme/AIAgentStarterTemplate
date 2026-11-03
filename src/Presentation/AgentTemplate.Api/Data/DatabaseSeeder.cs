using AgentTemplate.Domain.Entities;
using AgentTemplate.Domain.ValueObjects;
using AgentTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentTemplate.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        // Seed products if not already seeded (bypass query filter for seeding)
        if (!await db.Products.IgnoreQueryFilters().AnyAsync())
        {
            var product1 = new Product(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "tenant-default",
                new Sku("PROD-001"),
                "High-Performance AI Accelerator",
                "Dedicated neural compute accelerator for edge inferencing.",
                new Money(499.99m, "USD"),
                25);

            var product2 = new Product(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "tenant-default",
                new Sku("PROD-002"),
                "Ultra-Fast NVMe Storage 2TB",
                "High-end PCIe 5.0 solid state drive with 14GB/s sequential reads.",
                new Money(189.50m, "USD"),
                50);

            var product3 = new Product(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "tenant-default",
                new Sku("PROD-003"),
                "Mechanical Developer Keyboard",
                "Hot-swappable ergonomic keyboard with tactile switches.",
                new Money(129.00m, "USD"),
                100);

            await db.Products.AddRangeAsync(product1, product2, product3);

            // Seed demo order
            var demoOrder = new Order(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "tenant-default",
                "user-demo");

            demoOrder.AddItem(
                product1.Id,
                product1.Name,
                product1.Sku,
                1,
                product1.Price);

            demoOrder.AddItem(
                product2.Id,
                product2.Name,
                product2.Sku,
                2,
                product2.Price);

            await db.Orders.AddAsync(demoOrder);
            await db.SaveChangesAsync();
        }
    }
}
