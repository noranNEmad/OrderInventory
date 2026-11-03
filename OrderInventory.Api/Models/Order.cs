
using OrderInventory.Api.Enums;

namespace OrderInventory.Api.Models;

public class Order
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}
