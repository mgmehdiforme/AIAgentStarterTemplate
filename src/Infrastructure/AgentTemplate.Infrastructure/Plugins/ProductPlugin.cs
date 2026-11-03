using System.ComponentModel;
using AgentTemplate.Application.Abstractions;
using AgentTemplate.Application.Dtos;
using Microsoft.SemanticKernel;

namespace AgentTemplate.Infrastructure.Plugins;

public sealed class ProductPlugin
{
    private readonly IProductRepository _productRepository;

    public ProductPlugin(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    [KernelFunction, Description("Searches the product catalog by name or keyword description.")]
    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(
        [Description("The search term or query string")] string query,
        CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.SearchAsync(query, 10, cancellationToken);

        return products.Select(p => new ProductDto(
            p.Id,
            p.Sku.Value,
            p.Name,
            p.Description,
            p.Price.Amount,
            p.Price.Currency,
            p.StockQuantity,
            p.IsActive)).ToList();
    }

    [KernelFunction, Description("Retrieves a product by its unique SKU code.")]
    public async Task<ProductDto?> GetProductBySkuAsync(
        [Description("The exact SKU code of the product (e.g. 'PROD-001')")] string sku,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetBySkuAsync(sku, cancellationToken);
        if (product == null)
            return null;

        return new ProductDto(
            product.Id,
            product.Sku.Value,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.IsActive);
    }
}
