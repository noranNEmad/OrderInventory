
using System.ComponentModel.DataAnnotations;

namespace OrderInventory.Api.DTOs;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one item.")]
    public List<CreateOrderItemDto> Items { get; set; }
        = new List<CreateOrderItemDto>();
}
