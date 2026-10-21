namespace SmallBizERP.Core.Entities;

public class Product
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid CategoryId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal BuyingPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public int MinimumStockLevel { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}