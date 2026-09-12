using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class DeleteAccountViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Confirm your password")]
        public string Password { get; set; } = "";
    }
}