namespace SmallBizERP.API.Models;

public class StockAdjustmentRequest
{
    public Guid BusinessId { get; set; }

    public Guid ProductId { get; set; }

    // Supported values: Receive, Damage, Correction
    public string Action { get; set; } = string.Empty;

    // Used for Receive and Damage; must be positive.
    public int Quantity { get; set; }

    // Used for Correction: the actual physical count.
    public int? PhysicalCount { get; set; }

    // Optional cost per unit for received stock.
    public decimal? UnitCost { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}
