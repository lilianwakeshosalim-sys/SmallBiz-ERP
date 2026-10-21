using SmallBizERP.Core.Enums;

namespace SmallBizERP.Core.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid? SaleId { get; set; }

    public Guid? PurchaseId { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Amount { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public Guid? CreatedByUserId { get; set; }
}