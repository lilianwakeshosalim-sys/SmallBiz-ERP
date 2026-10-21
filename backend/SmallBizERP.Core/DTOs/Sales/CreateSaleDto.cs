namespace SmallBizERP.Core.DTOs.Sales;

public class CreateSaleDto
{
    public Guid BusinessId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal AmountPaid { get; set; }

    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }
}