namespace SmallBizERP.Core.Entities;

public class Expense
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

    public string? Reference { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}