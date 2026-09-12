using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineComputerStore.Core.Data;
using OnlineComputerStore.Core.Models;

namespace OnlineComputerStore.Core.Services
{
    public class UserService : IUserService
    {
        private readonly StoreDbContext _context;
        public UserService(StoreDbContext context) => _context = context;

        public async Task<AppUser?> FindByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        public async Task<AppUser?> FindByIdAsync(int id) =>
            await _context.Users.FindAsync(id);

        public async Task<AppUser> RegisterAsync(string fullName, string email, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = HashPassword(password, salt);

            var user = new AppUser
            {
                FullName = fullName,
                Email = email,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<AppUser?> ValidateCredentialsAsync(string identifier, string password)
        {
            var user = await FindByEmailAsync(identifier);

            if (user == null)
            {
                // Fall back to matching by full name � but only if it's unambiguous.
                var nameMatches = await _context.Users
                    .Where(u => u.FullName.ToLower() == identifier.ToLower())
                    .ToListAsync();
                user = nameMatches.Count == 1 ? nameMatches[0] : null;
            }

            if (user == null) return null;

            var salt = Convert.FromBase64String(user.PasswordSalt);
            var hash = HashPassword(password, salt);
            return Convert.ToBase64String(hash) == user.PasswordHash ? user : null;
        }

        public async Task<bool> DeleteAccountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var savedCartItems = _context.SavedCartItems.Where(s => s.UserId == userId);
            _context.SavedCartItems.RemoveRange(savedCartItems);

            var wishlistItems = _context.WishlistItems.Where(w => w.UserId == userId);
            _context.WishlistItems.RemoveRange(wishlistItems);

            var resetTokens = _context.PasswordResetTokens.Where(t => t.UserId == userId);
            _context.PasswordResetTokens.RemoveRange(resetTokens);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId, string fullName, string email, string phone, string address)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Account not found.");

            var emailTaken = await _context.Users.AnyAsync(u => u.Id != userId && u.Email.ToLower() == email.ToLower());
            if (emailTaken) return (false, "That email is already used by another account.");

            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.Address = address;
            await _context.SaveChangesAsync();
            return (true, "Profile updated.");
        }

        public async Task<bool> UpdateProfilePhotoAsync(int userId, string? photoDataUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.ProfilePhotoUrl = photoDataUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Account not found.");

            var salt = Convert.FromBase64String(user.PasswordSalt);
            var currentHash = HashPassword(currentPassword, salt);
            if (Convert.ToBase64String(currentHash) != user.PasswordHash)
                return (false, "Your current password wasn't correct.");

            var newSalt = RandomNumberGenerator.GetBytes(16);
            var newHash = HashPassword(newPassword, newSalt);
            user.PasswordSalt = Convert.ToBase64String(newSalt);
            user.PasswordHash = Convert.ToBase64String(newHash);
            await _context.SaveChangesAsync();
            return (true, "Password changed.");
        }

        public async Task<string> GeneratePasswordResetTokenAsync(int userId)
        {
            // Only the newest link should work — clear out any unused ones first.
            var oldTokens = _context.PasswordResetTokens.Where(t => t.UserId == userId && !t.Used);
            _context.PasswordResetTokens.RemoveRange(oldTokens);

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Used = false
            });
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string email, string newPassword)
        {
            var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (resetToken == null || resetToken.Used || resetToken.ExpiresAt < DateTime.UtcNow)
                return (false, "This password reset link is invalid or has expired. Please request a new one.");

            var user = await _context.Users.FindAsync(resetToken.UserId);
            if (user == null || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                return (false, "This password reset link is invalid or has expired. Please request a new one.");

            var newSalt = RandomNumberGenerator.GetBytes(16);
            var newHash = HashPassword(newPassword, newSalt);
            user.PasswordSalt = Convert.ToBase64String(newSalt);
            user.PasswordHash = Convert.ToBase64String(newHash);
            resetToken.Used = true;

            await _context.SaveChangesAsync();
            return (true, "Password reset.");
        }

        private static byte[] HashPassword(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }
}