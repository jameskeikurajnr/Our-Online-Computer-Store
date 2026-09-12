using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class VerifyEmailViewModel
    {
        [Required, EmailAddress, Display(Name = "Email address")]
        public string Email { get; set; } = "";
    }

    public class VerifyCodeViewModel
    {
        [Required, StringLength(6, MinimumLength = 6, ErrorMessage = "Enter the 6-digit code.")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "The code should be 6 digits.")]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = "";
    }
}
