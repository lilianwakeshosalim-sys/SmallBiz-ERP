using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseItemsController : ControllerBase
{
private readonly AppDbContext _context;


public PurchaseItemsController(AppDbContext context)
{
    _context = context;
}

// GET: api/purchaseitems
// GET: api/purchaseitems?purchaseId={purchaseId}
[HttpGet]
public async Task<ActionResult<IEnumerable<PurchaseItem>>> GetPurchaseItems(
    [FromQuery] Guid? purchaseId)
{
    var query = _context.PurchaseItems
        .AsNoTracking()
        .AsQueryable();

    if (purchaseId.HasValue)
    {
        query = query.Where(
            item => item.PurchaseId == purchaseId.Value);
    }

    var items = await query.ToListAsync();

    return Ok(items);
}

// GET: api/purchaseitems/{id}
[HttpGet("{id:guid}")]
public async Task<ActionResult<PurchaseItem>> GetPurchaseItem(Guid id)
{
    var item = await _context.PurchaseItems
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);

    if (item == null)
        return NotFound("Purchase item not found.");

    return Ok(item);
}

// POST: api/purchaseitems
[HttpPost]
public async Task<ActionResult<PurchaseItem>> CreatePurchaseItem(
    PurchaseItem purchaseItem)
{
    if (purchaseItem.PurchaseId == Guid.Empty)
        return BadRequest("Purchase ID is required.");

    if (purchaseItem.ProductId == Guid.Empty)
        return BadRequest("Product ID is required.");

    if (purchaseItem.Quantity <= 0)
        return BadRequest("Quantity must be greater than zero.");

    if (purchaseItem.UnitCost < 0)
        return BadRequest("Unit cost cannot be negative.");

    var purchase = await _context.Purchases
        .FirstOrDefaultAsync(p =>
            p.Id == purchaseItem.PurchaseId);

    if (purchase == null)
        return NotFound("Purchase not found.");

    if (purchase.Status != PurchaseStatus.Pending)
    {
        return BadRequest(
            "Items can only be added to pending purchases.");
    }

    var product = await _context.Products
        .FirstOrDefaultAsync(p =>
            p.Id == purchaseItem.ProductId);

    if (product == null)
        return NotFound("Product not found.");

    if (purchase.BusinessId != product.BusinessId)
    {
        return BadRequest(
            "The product does not belong to the purchase's business.");
    }

    purchaseItem.Id = Guid.NewGuid();
    purchaseItem.Total =
        purchaseItem.Quantity * purchaseItem.UnitCost;

    // Calculate the subtotal including the new item.
    var existingItemsTotal = await _context.PurchaseItems
        .Where(item => item.PurchaseId == purchase.Id)
        .SumAsync(item => (decimal?)item.Total) ?? 0m;

    var newSubtotal = existingItemsTotal + purchaseItem.Total;

    // Preserve the purchase's discount and tax.
    if (purchase.Discount > newSubtotal)
    {
        return BadRequest(
            "The discount cannot exceed the updated subtotal.");
    }

    var newTotal = newSubtotal - purchase.Discount + purchase.Tax;

    if (purchase.AmountPaid > newTotal)
    {
        return BadRequest(
            "The updated total cannot be less than the amount already paid.");
    }

    purchase.Subtotal = newSubtotal;
    purchase.TotalAmount = newTotal;
    purchase.Balance = newTotal - purchase.AmountPaid;

    _context.PurchaseItems.Add(purchaseItem);

    // Save the item and updated purchase totals together.
    await using var transaction =
        await _context.Database.BeginTransactionAsync();

    try
    {
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }

    return CreatedAtAction(
        nameof(GetPurchaseItem),
        new { id = purchaseItem.Id },
        purchaseItem);
}


}
