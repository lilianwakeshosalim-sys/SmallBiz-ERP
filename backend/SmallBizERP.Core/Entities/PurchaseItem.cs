namespace SmallBizERP.Core.Entities;

public class PurchaseItem
{
    public Guid Id { get; set; }

    public Guid PurchaseId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Total { get; set; }
}