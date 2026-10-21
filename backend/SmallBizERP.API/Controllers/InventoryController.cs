using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/inventory?businessId={businessId}
    [HttpGet]
    public async Task<IActionResult> GetInventory([FromQuery] Guid businessId)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid businessId is required." });
        }

        var businessExists = await _context.Businesses
            .AnyAsync(business => business.Id == businessId);

        if (!businessExists)
        {
            return NotFound(new { message = "Business not found." });
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(product => product.BusinessId == businessId)
            .Select(product => new
            {
                product.Id,
                product.BusinessId,
                product.CategoryId,
                product.SKU,
                product.Name,
                product.Description,
                product.BuyingPrice,
                product.SellingPrice,
                product.MinimumStockLevel,
                product.IsActive,
                CurrentStock = _context.StockTransactions
                    .Where(transaction =>
                        transaction.BusinessId == businessId &&
                        transaction.ProductId == product.Id)
                    .Sum(transaction =>
                        transaction.TransactionType == StockTransactionType.Purchase ||
                        transaction.TransactionType == StockTransactionType.OpeningStock ||
                        transaction.TransactionType == StockTransactionType.Return
                            ? transaction.Quantity
                            : transaction.TransactionType == StockTransactionType.Sale ||
                              transaction.TransactionType == StockTransactionType.Damage
                                ? -transaction.Quantity
                                : transaction.Quantity)
            })
            .ToListAsync();

        var inventory = products.Select(product => new
        {
            product.Id,
            product.BusinessId,
            product.CategoryId,
            product.SKU,
            product.Name,
            product.Description,
            product.BuyingPrice,
            product.SellingPrice,
            product.MinimumStockLevel,
            product.IsActive,
            product.CurrentStock,
            StockStatus = product.CurrentStock <= 0
                ? "Out of Stock"
                : product.CurrentStock <= product.MinimumStockLevel
                    ? "Low Stock"
                    : "In Stock"
        });

        return Ok(inventory);
    }
}
