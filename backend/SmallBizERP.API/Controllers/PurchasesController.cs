using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
private readonly AppDbContext _context;


public PurchasesController(AppDbContext context)
{
    _context = context;
}

// GET: api/purchases
// GET: api/purchases?businessId={businessId}
[HttpGet]
public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases(
    [FromQuery] Guid? businessId)
{
    var query = _context.Purchases
        .AsNoTracking()
        .AsQueryable();

    if (businessId.HasValue)
    {
        query = query.Where(
            p => p.BusinessId == businessId.Value);
    }

    var purchases = await query
        .OrderByDescending(p => p.PurchaseDate)
        .ToListAsync();

    return Ok(purchases);
}

// GET: api/purchases/{id}
[HttpGet("{id:guid}")]
public async Task<ActionResult<Purchase>> GetPurchase(Guid id)
{
    var purchase = await _context.Purchases
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    if (purchase == null)
        return NotFound("Purchase not found.");

    return Ok(purchase);
}

// POST: api/purchases
[HttpPost]
public async Task<ActionResult<Purchase>> CreatePurchase(
    Purchase purchase)
{
    if (purchase.BusinessId == Guid.Empty)
        return BadRequest("Business ID is required.");

    if (purchase.SupplierId == Guid.Empty)
        return BadRequest("Supplier ID is required.");

    if (string.IsNullOrWhiteSpace(purchase.PurchaseNumber))
        return BadRequest("Purchase number is required.");

    if (purchase.PurchaseDate == default)
        return BadRequest("Purchase date is required.");

    if (purchase.Subtotal < 0 ||
        purchase.Discount < 0 ||
        purchase.Tax < 0 ||
        purchase.TotalAmount < 0 ||
        purchase.AmountPaid < 0)
    {
        return BadRequest(
            "Purchase amounts cannot be negative.");
    }

    if (purchase.Discount > purchase.Subtotal)
        return BadRequest(
            "Discount cannot exceed the subtotal.");

    var expectedTotal =
        purchase.Subtotal - purchase.Discount + purchase.Tax;

    if (purchase.TotalAmount != expectedTotal)
    {
        return BadRequest(
            "Total amount must equal subtotal minus discount plus tax.");
    }

    if (purchase.AmountPaid > purchase.TotalAmount)
    {
        return BadRequest(
            "Amount paid cannot exceed the total amount.");
    }

    var businessExists = await _context.Businesses
        .AnyAsync(b => b.Id == purchase.BusinessId);

    if (!businessExists)
        return NotFound("Business not found.");

    var supplierExists = await _context.Suppliers
        .AnyAsync(s =>
            s.Id == purchase.SupplierId &&
            s.BusinessId == purchase.BusinessId);

    if (!supplierExists)
    {
        return BadRequest(
            "Supplier not found for this business.");
    }

    var numberExists = await _context.Purchases
        .AnyAsync(p =>
            p.BusinessId == purchase.BusinessId &&
            p.PurchaseNumber == purchase.PurchaseNumber);

    if (numberExists)
    {
        return Conflict(
            "This purchase number already exists for this business.");
    }
if (!Enum.IsDefined(typeof(SmallBizERP.Core.Enums.PurchaseStatus), purchase.Status))
{
    return BadRequest("Invalid purchase status.");
}
if (purchase.Status != SmallBizERP.Core.Enums.PurchaseStatus.Pending)
{
    return BadRequest("New purchases must have Pending status.");
}
    purchase.Id = Guid.NewGuid();
    purchase.Balance =
        purchase.TotalAmount - purchase.AmountPaid;
    purchase.CreatedAt = DateTime.UtcNow;

    _context.Purchases.Add(purchase);
    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetPurchase),
        new { id = purchase.Id },
        purchase);
}

}
