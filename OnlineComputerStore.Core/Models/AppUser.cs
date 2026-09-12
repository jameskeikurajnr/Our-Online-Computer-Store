namespace OnlineComputerStore.Core.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
        // The very first account ever registered is auto-promoted to admin by
        // DbInitializer — see there for how to hand admin to someone else instead.
        public bool IsAdmin { get; set; } = false;

        // Stored as a data: URL (e.g. "data:image/jpeg;base64,...") rather than a
        // file path — keeps the whole account self-contained in the database row,
        // with nothing on disk to orphan when an account is deleted. Null/empty
        // means no photo — UI falls back to an initials avatar.
        public string? ProfilePhotoUrl { get; set; }
    }
}