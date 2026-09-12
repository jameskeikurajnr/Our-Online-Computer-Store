using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class ContactViewModel
    {
        [Required, Display(Name = "Your name")]
        public string Name { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Subject { get; set; } = "";

        [Required, StringLength(2000)]
        public string Message { get; set; } = "";
    }
}
