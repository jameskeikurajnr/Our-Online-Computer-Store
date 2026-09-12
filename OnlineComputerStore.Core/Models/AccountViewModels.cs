using System.ComponentModel.DataAnnotations;

namespace OnlineComputerStore.Core.Models
{
    public class RegisterViewModel
    {
        [Required, Display(Name = "Full name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = "";
    }

    public class LoginViewModel
    {
        [Required, Display(Name = "Email or Name")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password), MinLength(6), Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [Required, DataType(DataType.Password), Compare("NewPassword"), Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";
    }
}
