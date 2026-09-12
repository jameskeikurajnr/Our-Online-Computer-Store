using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Components
{
    // Renders the signed-in user's profile photo, or a teal initials circle when
    // they don't have one set. Looks the user up by id on every render (like
    // CartBadge/WishlistBadge) rather than via a claim, since a photo — even a
    // small one — is far too big to carry in the auth cookie.
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly IUserService _users;
        public UserAvatarViewComponent(IUserService users) => _users = users;

        public async Task<IViewComponentResult> InvokeAsync(int size = 34)
        {
            var fontSize = System.Math.Max(11, (int)(size * 0.42));

            var user = HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return View(new UserAvatarViewModel(null, "?", size, fontSize));

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
                return View(new UserAvatarViewModel(null, "?", size, fontSize));

            var account = await _users.FindByIdAsync(userId);
            var initials = Initials(account?.FullName ?? user.Identity.Name ?? "?");
            return View(new UserAvatarViewModel(account?.ProfilePhotoUrl, initials, size, fontSize));
        }

        private static string Initials(string fullName)
        {
            var parts = fullName.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
        }
    }

    public record UserAvatarViewModel(string? PhotoUrl, string Initials, int Size, int FontSize);
}
