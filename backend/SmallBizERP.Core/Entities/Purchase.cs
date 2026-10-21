using SmallBizERP.Core.Enums;

namespace SmallBizERP.Core.Entities;

public class Purchase
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid SupplierId { get; set; }

    public string PurchaseNumber { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}