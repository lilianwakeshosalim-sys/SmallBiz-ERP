
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Enums;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{businessId:guid}")]
    public async Task<IActionResult> GetDashboard(Guid businessId)
    {
        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == businessId);

        if (!businessExists)
        {
            return NotFound("Business not found.");
        }

        var sales = _context.Sales
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId);

        var completedSales = sales
            .Where(s => s.Status == SaleStatus.Completed);

        var totalSales = await completedSales.CountAsync();

        var totalRevenue = await completedSales
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;

        var totalPaymentsReceived = await _context.Payments
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var outstandingSalesBalance = await sales
            .Where(s => s.Status != SaleStatus.Cancelled &&
                        s.Status != SaleStatus.Refunded)
            .SumAsync(s => (decimal?)s.Balance) ?? 0m;

        var inventory = await _context.StockTransactions
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .GroupBy(t => t.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                CurrentStock = g.Sum(t =>
                    t.TransactionType == StockTransactionType.Sale ||
                    t.TransactionType == StockTransactionType.Damage
                        ? -t.Quantity
                        : t.TransactionType == StockTransactionType.Adjustment
                            ? t.Quantity
                            : t.Quantity)
            })
            .ToListAsync();

        var activeProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.MinimumStockLevel
            })
            .ToListAsync();

        var lowStockProducts = activeProducts.Count(p =>
        {
            var stock = inventory
                .FirstOrDefault(i => i.ProductId == p.Id)?.CurrentStock ?? 0;

            return stock <= p.MinimumStockLevel;
        });

        var totalProducts = activeProducts.Count;

        return Ok(new
        {
            BusinessId = businessId,
            TotalSales = totalSales,
            TotalRevenue = totalRevenue,
            TotalPaymentsReceived = totalPaymentsReceived,
            OutstandingSalesBalance = outstandingSalesBalance,
            TotalProducts = totalProducts,
            LowStockProducts = lowStockProducts
        });
    }
}