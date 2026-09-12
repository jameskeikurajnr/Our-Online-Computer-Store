using System.Threading.Tasks;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public interface IUserService
    {
        Task<AppUser?> FindByEmailAsync(string email);
        Task<AppUser?> FindByIdAsync(int id);
        Task<AppUser> RegisterAsync(string fullName, string email, string password);
        Task<AppUser?> ValidateCredentialsAsync(string email, string password);
        Task<bool> DeleteAccountAsync(int userId);
        Task<(bool Success, string Message)> UpdateProfileAsync(int userId, string fullName, string email, string phone, string address);

        // Pass a data: URL to set/replace the photo, or null to remove it.
        Task<bool> UpdateProfilePhotoAsync(int userId, string? photoDataUrl);
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        // Issues a fresh, single-use reset token for this user (invalidating any
        // unused ones still outstanding) — the caller emails it, this service
        // never sends mail itself.
        Task<string> GeneratePasswordResetTokenAsync(int userId);

        Task<(bool Success, string Message)> ResetPasswordAsync(string token, string email, string newPassword);
    }
}