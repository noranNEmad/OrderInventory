namespace OrderInventory.Api.DTOs.Reports;

public class BestSellingProductDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int TotalQuantitySold { get; set; }
}