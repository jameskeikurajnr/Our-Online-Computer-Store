using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "New password"), MinLength(6)]
        public string NewPassword { get; set; } = "";

        [Required, DataType(DataType.Password), Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation do not match.")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}