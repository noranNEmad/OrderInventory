
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Data;
using OrderInventory.Api.DTOs;
using OrderInventory.Api.Models;
using OrderInventory.Api.Enums;
using Microsoft.AspNetCore.RateLimiting;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("GeneralPolicy")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        AppDbContext context,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> Create(
        CreateOrderDto newOrder)
    {
        using (_logger.BeginScope("Operation: CreateOrder"))
        {
            // 1. Check for duplicate products
            var duplicateProductIds = newOrder.Items
                .GroupBy(i => i.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateProductIds.Count > 0)
            {
                _logger.LogWarning(
                    "Order creation rejected because duplicate products were submitted. ProductIds: {ProductIds}",
                    duplicateProductIds);

                return BadRequest(
                    "Each product can only appear once in an order.");
            }

            // 2. Get all requested products
            var productIds = newOrder.Items
                .Select(i => i.ProductId)
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // 3. Check that all products exist
            if (products.Count != productIds.Count)
            {
                var missingProductIds = productIds
                    .Except(products.Select(p => p.Id))
                    .ToList();

                _logger.LogWarning(
                    "Order creation rejected because products were not found. ProductIds: {ProductIds}",
                    missingProductIds);

                return BadRequest(
                    "One or more products do not exist.");
            }

            // 4. Start transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order();

                // 5. Atomically check and reduce stock
                foreach (var item in newOrder.Items)
                {
                    var product = products
                        .First(p => p.Id == item.ProductId);

                    var affectedRows = await _context.Products
                        .Where(p =>
                            p.Id == item.ProductId &&
                            p.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(setters =>
                            setters.SetProperty(
                                p => p.StockQuantity,
                                p => p.StockQuantity - item.Quantity));

                    // 6. Handle insufficient stock
                    if (affectedRows == 0)
                    {
                        await transaction.RollbackAsync();

                        _logger.LogWarning(
                            "Order creation failed because product {ProductId} has insufficient stock. Requested quantity: {Quantity}",
                            product.Id,
                            item.Quantity);

                        return BadRequest(
                            $"Insufficient stock for product: {product.Name}");
                    }

                    // 7. Create order item
                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    };

                    order.Items.Add(orderItem);

                    // 8. Calculate total
                    order.TotalAmount +=
                        product.Price * item.Quantity;
                }

                // 9. Save order
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                // 10. Commit transaction
                await transaction.CommitAsync();

                // 11. Log successful order creation
                _logger.LogInformation(
                    "Order {OrderId} was created successfully. Total amount: {TotalAmount}",
                    order.Id,
                    order.TotalAmount);

                // 12. Build response
                var response = new OrderResponseDto
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,

                    Items = order.Items.Select(i =>
                        new OrderItemResponseDto
                        {
                            ProductId = i.ProductId,
                            ProductName = products
                                .First(p => p.Id == i.ProductId).Name,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            Subtotal = i.UnitPrice * i.Quantity
                        }).ToList()
                };

                return Created(
                    $"/api/Orders/{order.Id}",
                    response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "An error occurred while creating an order.");

                throw;
            }
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound("Order not found.");
        }

        var response = new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,

            Items = order.Items.Select(item =>
                new OrderItemResponseDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product!.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.UnitPrice * item.Quantity
                }).ToList()
        };

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponseDto<OrderResponseDto>>> GetAll(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // 1. Validate pagination parameters
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(
                "Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        // 2. Build query
        var query = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        // 3. Filter by status
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // 4. Get total count BEFORE pagination
        var totalCount = await query.CountAsync();

        // 5. Calculate total pages
        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        // 6. Apply pagination
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 7. Map to DTOs
        var responseItems = orders.Select(order =>
            new OrderResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,

                Items = order.Items.Select(item =>
                    new OrderItemResponseDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product!.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.UnitPrice * item.Quantity
                    }).ToList()
            }).ToList();

        // 8. Build pagination response
        var response = new PaginationResponseDto<OrderResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = responseItems
        };

        return Ok(response);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        using (_logger.BeginScope("OrderId: {OrderId}", id))
        {
            _logger.LogInformation(
                "Starting order cancellation.");

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                _logger.LogWarning(
                    "Order was not found.");

                return NotFound("Order not found.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Order is already cancelled.");

                return BadRequest("Order is already cancelled.");
            }

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning(
                    "Order cannot be cancelled because its status is {Status}.",
                    order.Status);

                return BadRequest(
                    "Only pending orders can be cancelled.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Restore product stock
                foreach (var item in order.Items)
                {
                    item.Product!.StockQuantity += item.Quantity;
                }

                // 2. Update order status
                order.Status = OrderStatus.Cancelled;

                // 3. Save changes
                await _context.SaveChangesAsync();

                // 4. Commit transaction
                await transaction.CommitAsync();

                // 5. Log successful cancellation
                _logger.LogInformation(
                    "Order was cancelled successfully. Order total: {TotalAmount}",
                    order.TotalAmount);

                return Ok(new
                {
                    message = "Order cancelled successfully.",
                    orderId = order.Id,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "An error occurred while cancelling the order.");

                throw;
            }
        }
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        using (_logger.BeginScope("OrderId: {OrderId}", id))
        {
            _logger.LogInformation(
                "Starting order confirmation.");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                _logger.LogWarning(
                    "Order was not found.");

                return NotFound("Order not found.");
            }

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning(
                    "Order cannot be confirmed because its status is {Status}.",
                    order.Status);

                return BadRequest(
                    "Only pending orders can be confirmed.");
            }

            order.Status = OrderStatus.Confirmed;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Order was confirmed successfully.");

            return Ok(new
            {
                Message = "Order confirmed successfully.",
                OrderId = order.Id,
                Status = order.Status
            });
        }
    }
}
