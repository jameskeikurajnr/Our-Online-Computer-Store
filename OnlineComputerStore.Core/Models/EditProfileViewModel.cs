using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineComputerStore.Core.Models
{
    public class EditProfileViewModel
    {
        [Required, Display(Name = "Full name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Display(Name = "Phone number")]
        public string Phone { get; set; } = "";

        [Display(Name = "Address")]
        public string Address { get; set; } = "";

        // Not posted back on validation failure (file inputs can't be
        // repopulated by the browser) — the controller re-reads it from the
        // database whenever it needs to redisplay the form.
        public string? CurrentPhotoUrl { get; set; }

        [Display(Name = "Profile photo")]
        public IFormFile? ProfilePhoto { get; set; }

        [Display(Name = "Remove current photo")]
        public bool RemovePhoto { get; set; }
    }
}