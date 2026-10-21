using SmallBizERP.Core.Enums;

namespace SmallBizERP.Core.Entities;

public class Sale
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid? CustomerId { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}