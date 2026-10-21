using SmallBizERP.Core.Enums;

namespace SmallBizERP.Core.Entities;

public class StockTransaction
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid ProductId { get; set; }

    public StockTransactionType TransactionType { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; set; }
}