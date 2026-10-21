
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Core.Enums;
using SmallBizERP.Core.DTOs.Sales;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/sales
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
    {
        var sales = await _context.Sales
            .AsNoTracking()
            .ToListAsync();

        return Ok(sales);
    }

    // POST: api/sales
    [HttpPost]
    public async Task<IActionResult> CreateSale(CreateSaleDto request)
    {
        if (request.BusinessId == Guid.Empty)
            return BadRequest("Business ID is required.");

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest("A sale must contain at least one item.");

        if (request.Discount < 0 || request.Tax < 0 ||
            request.AmountPaid < 0)
            return BadRequest("Discount, tax and payment cannot be negative.");

        if (request.Items.Any(item =>
            item.ProductId == Guid.Empty ||
            item.Quantity <= 0 ||
            item.UnitPrice < 0 ||
            item.Discount < 0))
            return BadRequest("Each item must have a valid product, positive quantity and non-negative prices and discount.");

        if (request.Items.GroupBy(item => item.ProductId)
            .Any(group => group.Count() > 1))
            return BadRequest("Each product should appear only once in a sale.");

        var productIds = request.Items
            .Select(item => item.ProductId)
            .ToList();

        var products = await _context.Products
            .Where(product =>
                product.BusinessId == request.BusinessId &&
                product.IsActive &&
                productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        if (products.Count != productIds.Count)
            return BadRequest("One or more products are invalid or inactive for this business.");

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers.AnyAsync(customer =>
                customer.Id == request.CustomerId.Value &&
                customer.BusinessId == request.BusinessId);

            if (!customerExists)
                return BadRequest("Customer was not found for this business.");
        }

        var stockBalances = await _context.StockTransactions
            .Where(transaction =>
                transaction.BusinessId == request.BusinessId &&
                productIds.Contains(transaction.ProductId))
            .GroupBy(transaction => transaction.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Stock = group.Sum(transaction =>
                    transaction.TransactionType == StockTransactionType.Purchase ||
                    transaction.TransactionType == StockTransactionType.OpeningStock ||
                    transaction.TransactionType == StockTransactionType.Return
                        ? transaction.Quantity
                        : transaction.TransactionType == StockTransactionType.Sale ||
                          transaction.TransactionType == StockTransactionType.Damage
                            ? -transaction.Quantity
                            : 0)
            })
            .ToDictionaryAsync(result => result.ProductId, result => result.Stock);

        foreach (var item in request.Items)
        {
            var availableStock = stockBalances.GetValueOrDefault(item.ProductId);

            if (item.Quantity > availableStock)
            {
                return BadRequest(
                    $"Insufficient stock for {products[item.ProductId].Name}. " +
                    $"Available: {availableStock}; requested: {item.Quantity}.");
            }

            if (item.Discount > item.Quantity * item.UnitPrice)
                return BadRequest("An item discount cannot exceed its line value.");
        }

        var subtotal = request.Items.Sum(item =>
            item.Quantity * item.UnitPrice - item.Discount);

        if (request.Discount > subtotal)
            return BadRequest("Sale discount cannot exceed the subtotal.");

        var total = subtotal - request.Discount + request.Tax;

        if (request.AmountPaid > total)
            return BadRequest("Amount paid cannot exceed the sale total.");

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            CustomerId = request.CustomerId,
            SaleNumber = $"SALE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SaleDate = DateTime.UtcNow,
            Subtotal = subtotal,
            Discount = request.Discount,
            Tax = request.Tax,
            TotalAmount = total,
            AmountPaid = request.AmountPaid,
            Balance = total - request.AmountPaid,
            Status = request.AmountPaid == total
                ? SaleStatus.Completed
                : request.AmountPaid > 0
                    ? SaleStatus.PartiallyPaid
                    : SaleStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        _context.Sales.Add(sale);

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var lineTotal = item.Quantity * item.UnitPrice - item.Discount;

            _context.SaleItems.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitCost = product.BuyingPrice,
                Discount = item.Discount,
                Total = lineTotal
            });

            _context.StockTransactions.Add(new StockTransaction
            {
                Id = Guid.NewGuid(),
                BusinessId = request.BusinessId,
                ProductId = product.Id,
                TransactionType = StockTransactionType.Sale,
                Quantity = item.Quantity,
                UnitCost = product.BuyingPrice,
                ReferenceNumber = sale.SaleNumber,
                Notes = $"Stock issued for sale {sale.SaleNumber}",
                TransactionDate = DateTime.UtcNow
            });
        }

        if (request.AmountPaid > 0)
        {
            _context.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                BusinessId = request.BusinessId,
                SaleId = sale.Id,
                PaymentMethod = PaymentMethod.Cash,
                Amount = request.AmountPaid,
                ReferenceNumber = sale.SaleNumber,
                PaymentDate = DateTime.UtcNow,
                Notes = "Payment recorded when sale was created."
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(
            nameof(GetSales),
            new { id = sale.Id },
            new
            {
                sale.Id,
                sale.SaleNumber,
                sale.Subtotal,
                sale.Discount,
                sale.Tax,
                sale.TotalAmount,
                sale.AmountPaid,
                sale.Balance,
                sale.Status
            });
    }
}