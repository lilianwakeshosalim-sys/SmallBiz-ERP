using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
        [FromQuery] Guid? businessId)
    {
        var query = _context.Products.AsNoTracking();

        if (businessId.HasValue)
        {
            query = query.Where(p => p.BusinessId == businessId.Value);
        }

        return Ok(await query.ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(
        [FromQuery] Guid businessId,
        [FromBody] CreateProductRequest request)
    {
        if (businessId == Guid.Empty)
            return BadRequest("A valid business ID is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Product name is required.");

        if (string.IsNullOrWhiteSpace(request.SKU))
            return BadRequest("SKU is required.");

        if (request.CategoryId == Guid.Empty)
            return BadRequest("Please select a category.");

        if (request.BuyingPrice < 0 || request.SellingPrice < 0)
            return BadRequest("Prices cannot be negative.");

        if (request.MinimumStockLevel < 0 || request.OpeningStock < 0)
            return BadRequest("Stock quantities cannot be negative.");

        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == businessId);

        if (!businessExists)
            return BadRequest("The selected business does not exist.");

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId
                        && c.BusinessId == businessId
                        && c.IsActive);

        if (!categoryExists)
            return BadRequest("Please select an active category belonging to this business.");

        var sku = request.SKU.Trim();

        var skuExists = await _context.Products
            .AnyAsync(p => p.BusinessId == businessId && p.SKU == sku);

        if (skuExists)
            return Conflict("A product with this SKU already exists in this business.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CategoryId = request.CategoryId,
            SKU = sku,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            BuyingPrice = request.BuyingPrice,
            SellingPrice = request.SellingPrice,
            MinimumStockLevel = request.MinimumStockLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Products.Add(product);

            if (request.OpeningStock > 0)
            {
                _context.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    ProductId = product.Id,
                    TransactionType = StockTransactionType.OpeningStock,
                    Quantity = request.OpeningStock,
                    UnitCost = request.BuyingPrice,
                    ReferenceNumber = "OPENING-" + product.SKU,
                    Notes = "Opening stock recorded when product was created.",
                    TransactionDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetProducts),
                new { businessId },
                product);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Conflict("The product could not be saved. Check that its SKU is unique and try again.");
        }
    }
}

public class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int MinimumStockLevel { get; set; } = 5;
    public int OpeningStock { get; set; }
}
