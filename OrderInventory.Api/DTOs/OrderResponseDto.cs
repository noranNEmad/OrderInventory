
using OrderInventory.Api.Enums;

namespace OrderInventory.Api.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemResponseDto> Items { get; set; }
        = new List<OrderItemResponseDto>();
}
