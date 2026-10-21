
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallBizERP.Core.Entities;
using SmallBizERP.Infrastructure.Data;

namespace SmallBizERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ExpensesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/expenses
    // Optional filters: businessId, startDate, endDate
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(
        [FromQuery] Guid? businessId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue &&
            endDate.HasValue &&
            startDate.Value.Date > endDate.Value.Date)
        {
            return BadRequest(
                "Start date cannot be after end date.");
        }

        var query = _context.Expenses
            .AsNoTracking()
            .AsQueryable();

        if (businessId.HasValue)
        {
            query = query.Where(
                e => e.BusinessId == businessId.Value);
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(
                e => e.ExpenseDate >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);

            query = query.Where(
                e => e.ExpenseDate < endExclusive);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return Ok(expenses);
    }

    // GET: api/expenses/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Expense>> GetExpense(Guid id)
    {
        var expense = await _context.Expenses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null)
        {
            return NotFound("Expense not found.");
        }

        return Ok(expense);
    }


    // GET: api/expenses/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetExpenseSummary(
        [FromQuery] Guid? businessId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        if (startDate.HasValue &&
            endDate.HasValue &&
            startDate.Value.Date > endDate.Value.Date)
        {
            return BadRequest(
                "Start date cannot be after end date.");
        }

        var query = _context.Expenses
            .AsNoTracking()
            .AsQueryable();

        if (businessId.HasValue)
        {
            query = query.Where(
                e => e.BusinessId == businessId.Value);
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(
                e => e.ExpenseDate >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = endDate.Value.Date.AddDays(1);

            query = query.Where(
                e => e.ExpenseDate < endExclusive);
        }

        var totalExpenses = await query.CountAsync();

        var totalAmount = await query
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        var averageAmount = await query
            .AverageAsync(e => (decimal?)e.Amount) ?? 0m;

        return Ok(new
        {
            BusinessId = businessId,
            TotalExpenseRecords = totalExpenses,
            TotalExpenseAmount = totalAmount,
            AverageExpenseAmount = Math.Round(
                averageAmount, 2)
        });
    }

    // POST: api/expenses
    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense(
        Expense expense)
    {
        if (expense.BusinessId == Guid.Empty)
        {
            return BadRequest("Business ID is required.");
        }

        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == expense.BusinessId);

        if (!businessExists)
        {
            return BadRequest(
                "The specified business does not exist.");
        }

        if (string.IsNullOrWhiteSpace(expense.Description))
        {
            return BadRequest("Expense description is required.");
        }

        if (expense.Amount <= 0)
        {
            return BadRequest(
                "Expense amount must be greater than zero.");
        }

        expense.Id = Guid.NewGuid();

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetExpense),
            new { id = expense.Id },
            expense);
    }
}