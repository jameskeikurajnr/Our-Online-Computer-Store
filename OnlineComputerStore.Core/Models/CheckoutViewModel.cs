using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class CheckoutViewModel
    {
        [Required, Display(Name = "Full name")]
        public string CustomerName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        // "Ship" or "Pickup" — Address is required only for Ship, PickupLocation
        // only for Pickup. That's a conditional rule DataAnnotations can't express
        // cleanly, so both are validated by hand in CheckoutController instead of
        // with [Required] here.
        public string FulfillmentMethod { get; set; } = "Ship";

        [Display(Name = "Shipping address")]
        public string Address { get; set; } = "";

        [Display(Name = "Pickup location")]
        public string? PickupLocation { get; set; }
    }
}
