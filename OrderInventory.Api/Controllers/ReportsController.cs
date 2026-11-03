using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Data;
using OrderInventory.Api.DTOs.Reports;
using OrderInventory.Api.Enums;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport()
    {
        var totalOrders = await _context.Orders.CountAsync();

        var pendingOrders = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending);

        var confirmedOrders = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Confirmed);

        var cancelledOrders = await _context.Orders
            .CountAsync(o => o.Status == OrderStatus.Cancelled);

        var totalSales = await _context.Orders
            .Where(o => o.Status == OrderStatus.Confirmed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var report = new SalesReportDto
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            ConfirmedOrders = confirmedOrders,
            CancelledOrders = cancelledOrders,
            TotalSales = totalSales
        };

        return Ok(report);
    }

    [HttpGet("best-selling-products")]
    public async Task<ActionResult<IEnumerable<BestSellingProductDto>>>
    GetBestSellingProducts()
    {
        var report = await _context.OrderItems
            .Where(oi => oi.Order!.Status == OrderStatus.Confirmed)
            .GroupBy(oi => new
            {
                oi.ProductId,
                ProductName = oi.Product!.Name
            })
            .Select(group => new BestSellingProductDto
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                TotalQuantitySold = group.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .ToListAsync();

        return Ok(report);
    }
}