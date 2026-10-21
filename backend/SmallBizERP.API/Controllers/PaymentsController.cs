
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/payments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(
        [FromQuery] Guid? businessId)
    {
        var query = _context.Payments
            .AsNoTracking()
            .AsQueryable();

        if (businessId.HasValue)
        {
            query = query.Where(
                p => p.BusinessId == businessId.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return Ok(payments);
    }

    // GET: api/payments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound("Payment not found.");

        return Ok(payment);
    }

    // POST: api/payments
    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(
        Payment payment)
    {
        if (payment.BusinessId == Guid.Empty)
            return BadRequest("Business ID is required.");

        if (payment.Amount <= 0)
            return BadRequest(
                "Payment amount must be greater than zero.");

        if (!Enum.IsDefined(
            typeof(PaymentMethod), payment.PaymentMethod))
        {
            return BadRequest("Invalid payment method.");
        }

        // A payment must belong to exactly one transaction.
        bool hasSale = payment.SaleId.HasValue;
        bool hasPurchase = payment.PurchaseId.HasValue;

        if (hasSale == hasPurchase)
        {
            return BadRequest(
                "A payment must be linked to exactly one sale or purchase.");
        }

        Sale? sale = null;
        Purchase? purchase = null;
        decimal outstandingBalance;

        if (hasSale)
        {
            sale = await _context.Sales
                .FirstOrDefaultAsync(s =>
                    s.Id == payment.SaleId!.Value);

            if (sale == null)
                return NotFound("Sale not found.");

            if (sale.BusinessId != payment.BusinessId)
            {
                return BadRequest(
                    "The sale does not belong to this business.");
            }

            if (sale.Status == SaleStatus.Cancelled ||
                sale.Status == SaleStatus.Refunded)
            {
                return BadRequest(
                    "Payments cannot be added to a cancelled or refunded sale.");
            }

            outstandingBalance =
                sale.TotalAmount - sale.AmountPaid;

            if (outstandingBalance <= 0)
            {
                return BadRequest(
                    "This sale has no outstanding balance.");
            }
        }
        else
        {
            purchase = await _context.Purchases
                .FirstOrDefaultAsync(p =>
                    p.Id == payment.PurchaseId!.Value);

            if (purchase == null)
                return NotFound("Purchase not found.");

            if (purchase.BusinessId != payment.BusinessId)
            {
                return BadRequest(
                    "The purchase does not belong to this business.");
            }

            if (purchase.Status == PurchaseStatus.Cancelled ||
                purchase.Status == PurchaseStatus.Returned)
            {
                return BadRequest(
                    "Payments cannot be added to a cancelled or returned purchase.");
            }

            outstandingBalance =
                purchase.TotalAmount - purchase.AmountPaid;

            if (outstandingBalance <= 0)
            {
                return BadRequest(
                    "This purchase has no outstanding balance.");
            }
        }

        if (payment.Amount > outstandingBalance)
        {
            return BadRequest(
                $"Payment exceeds the outstanding balance of " +
                $"{outstandingBalance:F2}.");
        }

        // Save the payment and balance changes atomically.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        payment.Id = Guid.NewGuid();

        if (sale != null)
        {
            sale.AmountPaid += payment.Amount;
            sale.Balance =
                sale.TotalAmount - sale.AmountPaid;

            sale.Status = sale.Balance == 0
                ? SaleStatus.Completed
                : SaleStatus.PartiallyPaid;
        }
        else if (purchase != null)
        {
            purchase.AmountPaid += payment.Amount;
            purchase.Balance =
                purchase.TotalAmount - purchase.AmountPaid;

            // PurchaseStatus is intentionally unchanged.
        }

        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = payment.Id },
            payment);
    }
}