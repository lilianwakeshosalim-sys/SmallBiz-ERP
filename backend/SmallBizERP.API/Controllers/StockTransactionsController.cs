using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.API.Models;
using SmallBizERP.Core.Entities;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockTransactionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StockTransactionsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/stocktransactions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockTransaction>>> GetStockTransactions()
    {
        var transactions = await _context.StockTransactions
            .AsNoTracking()
            .ToListAsync();

        return Ok(transactions);
    }

    // POST: api/stocktransactions
    [HttpPost]
    public async Task<ActionResult<StockTransaction>> CreateStockTransaction(
        StockTransaction transaction)
    {
        if (!Enum.IsDefined(typeof(StockTransactionType), transaction.TransactionType))
        {
            return BadRequest("Invalid stock transaction type.");
        }

        transaction.Id = Guid.NewGuid();

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetStockTransactions),
            new { id = transaction.Id },
            transaction);
    }

    // POST: api/stocktransactions/adjust
    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock(
        [FromBody] StockAdjustmentRequest request)
    {
        if (request.BusinessId == Guid.Empty || request.ProductId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid business ID and product ID are required." });
        }

        var action = request.Action.Trim().ToLowerInvariant();

        if (action != "receive" && action != "damage" && action != "correction")
        {
            return BadRequest(new { message = "Action must be Receive, Damage, or Correction." });
        }

        if (action == "correction")
        {
            if (!request.PhysicalCount.HasValue || request.PhysicalCount.Value < 0)
            {
                return BadRequest(new { message = "PhysicalCount must be zero or greater for a correction." });
            }
        }
        else if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be greater than zero." });
        }

        if (request.UnitCost.HasValue && request.UnitCost.Value < 0)
        {
            return BadRequest(new { message = "UnitCost cannot be negative." });
        }

        var businessExists = await _context.Businesses
            .AnyAsync(business => business.Id == request.BusinessId);

        if (!businessExists)
        {
            return NotFound(new { message = "Business not found." });
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(product =>
                product.Id == request.ProductId &&
                product.BusinessId == request.BusinessId);

        if (product == null)
        {
            return NotFound(new { message = "Product was not found for this business." });
        }

        if (!product.IsActive)
        {
            return BadRequest(new { message = "Stock cannot be adjusted for an inactive product." });
        }

        var currentStock = await _context.StockTransactions
            .Where(transaction =>
                transaction.BusinessId == request.BusinessId &&
                transaction.ProductId == request.ProductId)
            .SumAsync(transaction =>
                transaction.TransactionType == StockTransactionType.Purchase ||
                transaction.TransactionType == StockTransactionType.OpeningStock ||
                transaction.TransactionType == StockTransactionType.Return
                    ? transaction.Quantity
                    : transaction.TransactionType == StockTransactionType.Sale ||
                      transaction.TransactionType == StockTransactionType.Damage
                        ? -transaction.Quantity
                        : transaction.Quantity);

        int signedQuantity;
        StockTransactionType transactionType;
        string defaultNote;

        switch (action)
        {
            case "receive":
                signedQuantity = request.Quantity;
                transactionType = StockTransactionType.Adjustment;
                defaultNote = "Stock received through stock adjustment.";
                break;

            case "damage":
                if (request.Quantity > currentStock)
                {
                    return BadRequest(new
                    {
                        message = $"Cannot record damage of {request.Quantity} units. Current stock is {currentStock}."
                    });
                }

                signedQuantity = request.Quantity;
                transactionType = StockTransactionType.Damage;
                defaultNote = "Damaged stock recorded.";
                break;

            default:
                signedQuantity = request.PhysicalCount!.Value - currentStock;
                transactionType = StockTransactionType.Adjustment;
                defaultNote = "Stock corrected to match physical count.";

                if (signedQuantity == 0)
                {
                    return BadRequest(new
                    {
                        message = "Physical count already matches current stock. No adjustment is needed.",
                        currentStock
                    });
                }

                break;
        }

        if (action == "correction" && request.PhysicalCount!.Value < 0)
        {
            return BadRequest(new { message = "Physical count cannot be negative." });
        }

        var transaction = new StockTransaction
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            ProductId = request.ProductId,
            TransactionType = transactionType,
            Quantity = action == "damage" ? request.Quantity : signedQuantity,
            UnitCost = request.UnitCost ?? product.BuyingPrice,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber)
                ? $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : request.ReferenceNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? defaultNote
                : request.Notes.Trim(),
            TransactionDate = DateTime.UtcNow
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Stock adjustment recorded successfully.",
            transaction,
            previousStock = currentStock,
            newStock = currentStock + (transactionType == StockTransactionType.Damage
                ? -transaction.Quantity
                : transaction.Quantity)
        });
    }

    // PUT: api/stocktransactions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStockTransaction(
        Guid id,
        StockTransaction transaction)
    {
        if (id != transaction.Id)
        {
            return BadRequest("Transaction ID does not match.");
        }

        if (!Enum.IsDefined(typeof(StockTransactionType), transaction.TransactionType))
        {
            return BadRequest("Invalid stock transaction type.");
        }

        var existingTransaction = await _context.StockTransactions.FindAsync(id);

        if (existingTransaction == null)
        {
            return NotFound();
        }

        existingTransaction.BusinessId = transaction.BusinessId;
        existingTransaction.ProductId = transaction.ProductId;
        existingTransaction.TransactionType = transaction.TransactionType;
        existingTransaction.Quantity = transaction.Quantity;
        existingTransaction.UnitCost = transaction.UnitCost;
        existingTransaction.ReferenceNumber = transaction.ReferenceNumber;
        existingTransaction.Notes = transaction.Notes;
        existingTransaction.TransactionDate = transaction.TransactionDate;
        existingTransaction.CreatedByUserId = transaction.CreatedByUserId;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
