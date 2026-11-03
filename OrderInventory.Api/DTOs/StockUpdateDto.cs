using System.ComponentModel.DataAnnotations;

namespace OrderInventory.Api.DTOs;

public class StockUpdateDto
{
    [Range(1, 1000000)]
    public int Quantity { get; set; }
}