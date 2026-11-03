namespace OrderInventory.Api.DTOs.Reports;

public class SalesReportDto
{
    public int TotalOrders { get; set; }

    public decimal TotalSales { get; set; }

    public int PendingOrders { get; set; }

    public int ConfirmedOrders { get; set; }

    public int CancelledOrders { get; set; }
}