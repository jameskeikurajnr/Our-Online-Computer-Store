using System.Collections.Generic;
using System.Linq;

namespace OnlineComputerStore.Core.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Product.Price * i.Quantity);
    }
}
