namespace OnlineComputerStore.Core.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
