using AgentTemplate.Application.Abstractions;
using AgentTemplate.Domain.Entities;
using AgentTemplate.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgentTemplate.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUserContext;

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.TenantId).IsRequired().HasMaxLength(64);
            b.Property(p => p.Name).IsRequired().HasMaxLength(256);
            b.Property(p => p.Description).HasMaxLength(1024);
            b.Property(p => p.Sku)
                .HasConversion(sku => sku.Value, val => new Sku(val))
                .HasMaxLength(64)
                .IsRequired();
            b.OwnsOne(p => p.Price, money =>
            {
                money.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("PriceAmount");
                money.Property(m => m.Currency).HasMaxLength(3).HasColumnName("PriceCurrency");
            });

            // Global query filter for multi-tenancy
            if (_currentUserContext != null)
            {
                b.HasQueryFilter(p => p.TenantId == _currentUserContext.TenantId);
            }
        });

        // Order configuration
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.TenantId).IsRequired().HasMaxLength(64);
            b.Property(o => o.CustomerId).IsRequired().HasMaxLength(64);
            b.Property(o => o.CancellationReason).HasMaxLength(512);

            b.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            // Global query filter for multi-tenancy
            if (_currentUserContext != null)
            {
                b.HasQueryFilter(o => o.TenantId == _currentUserContext.TenantId);
            }
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
            b.Property(i => i.Sku)
                .HasConversion(sku => sku.Value, val => new Sku(val))
                .HasMaxLength(64)
                .IsRequired();
            b.OwnsOne(i => i.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("UnitPriceAmount");
                money.Property(m => m.Currency).HasMaxLength(3).HasColumnName("UnitPriceCurrency");
            });
        });
    }
}
