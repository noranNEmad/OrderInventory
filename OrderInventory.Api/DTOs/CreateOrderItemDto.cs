
using System.ComponentModel.DataAnnotations;

namespace OrderInventory.Api.DTOs;

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; }
}
