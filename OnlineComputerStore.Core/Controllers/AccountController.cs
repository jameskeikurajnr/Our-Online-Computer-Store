using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class AccountController : Controller
    {
        // Deliberately generous but bounded — a photo, not a document. Stored as
        // base64 in the database, so this also caps how much every profile
        // photo can bloat the row by (~1.4x this, once base64-encoded).
        private const long MaxProfilePhotoBytes = 1 * 1024 * 1024; // 1 MB
        private static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        // Categories that count toward the Personal Security Score on the account
        // page — owning at least one product from each raises the score.
        private static readonly string[] MfaCategory = { "MFA & Security Keys" };
        private static readonly string[] CyberCategory = { "Cybersecurity" };

        private readonly IUserService _users;
        private readonly ICartService _cart;
        private readonly IEmailService _email;
        private readonly IOrderService _orders;
        private readonly IProductService _products;
        public AccountController(IUserService users, ICartService cart, IEmailService email, IOrderService orders, IProductService products)
        {
            _users = users;
            _cart = cart;
            _email = email;
            _orders = orders;
            _products = products;
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _users.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "An account with that email already exists.");
                return View(model);
            }

            var user = await _users.RegisterAsync(model.FullName, model.Email, model.Password);
            await SignInAsync(user);
            _cart.MergeGuestCartIntoAccount(user.Id);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.ValidateCredentialsAsync(model.Email, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            await SignInAsync(user);
            _cart.MergeGuestCartIntoAccount(user.Id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [EnableRateLimiting("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _users.GeneratePasswordResetTokenAsync(user.Id);
                var resetUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme)!;
                await _email.SendPasswordResetAsync(user, resetUrl);
            }

            // Same confirmation whether or not the email was found — don't reveal
            // which addresses have accounts.
            TempData["ResetRequested"] = true;
            return RedirectToAction("ForgotPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, message) = await _users.ResetPasswordAsync(model.Token, model.Email, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            TempData["PasswordReset"] = true;
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Capture the user id before signing out — the claims are gone once
            // SignOutAsync runs. Snapshotting the saved cart into the session
            // means the now-signed-out browser keeps showing the same cart
            // (logging out shouldn't look like it emptied), while the saved
            // copy under this account stays intact for whenever they log back
            // in — even from a different browser or after this one is closed.
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
                _cart.SnapshotAccountCartToSession(userId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ManageAccount()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var orders = await _orders.GetByEmailAsync(email ?? "");
            var purchasedProductIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).ToHashSet();

            var ownedCategories = new HashSet<string>();
            if (purchasedProductIds.Count > 0)
            {
                var allProducts = await _products.GetAllAsync();
                ownedCategories = allProducts
                    .Where(p => purchasedProductIds.Contains(p.Id))
                    .Select(p => p.Category)
                    .ToHashSet();
            }

            var hasMfa = ownedCategories.Overlaps(MfaCategory);
            var hasCyber = ownedCategories.Overlaps(CyberCategory);

            ViewBag.HasMfaKey = hasMfa;
            ViewBag.HasCyberAccessory = hasCyber;
            ViewBag.SecurityScore = (hasMfa ? 50 : 0) + (hasCyber ? 50 : 0);

            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _users.FindByIdAsync(userId);
            if (user == null) return NotFound();

            return View(new EditProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                CurrentPhotoUrl = user.ProfilePhotoUrl
            });
        }

        [HttpPost]
        [Authorize]
        [RequestSizeLimit(2 * 1024 * 1024)] // a bit of headroom over MaxProfilePhotoBytes for the rest of the form
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                if (!AllowedPhotoContentTypes.Contains(model.ProfilePhoto.ContentType))
                    ModelState.AddModelError(nameof(model.ProfilePhoto), "Please upload a JPEG, PNG, GIF, or WEBP image.");
                else if (model.ProfilePhoto.Length > MaxProfilePhotoBytes)
                    ModelState.AddModelError(nameof(model.ProfilePhoto), "That photo is too large — please use one under 1 MB.");
            }

            if (!ModelState.IsValid)
            {
                model.CurrentPhotoUrl = (await _users.FindByIdAsync(userId))?.ProfilePhotoUrl;
                return View(model);
            }

            var (success, message) = await _users.UpdateProfileAsync(userId, model.FullName, model.Email, model.Phone, model.Address);

            if (!success)
            {
                ModelState.AddModelError("", message);
                model.CurrentPhotoUrl = (await _users.FindByIdAsync(userId))?.ProfilePhotoUrl;
                return View(model);
            }

            if (model.RemovePhoto)
            {
                await _users.UpdateProfilePhotoAsync(userId, null);
            }
            else if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                using var stream = new MemoryStream();
                await model.ProfilePhoto.CopyToAsync(stream);
                var dataUrl = $"data:{model.ProfilePhoto.ContentType};base64,{Convert.ToBase64String(stream.ToArray())}";
                await _users.UpdateProfilePhotoAsync(userId, dataUrl);
            }

            var updatedUser = await _users.FindByIdAsync(userId);
            await SignInAsync(updatedUser!); // refresh claims so the navbar shows the new name/email right away

            TempData["ProfileUpdated"] = true;
            return RedirectToAction("ManageAccount");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (success, message) = await _users.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(model);
            }

            TempData["PasswordChanged"] = true;
            return RedirectToAction("ManageAccount");
        }

        [HttpGet]
        [Authorize]
        public IActionResult DeleteAccount() => View(new DeleteAccountViewModel());

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = string.IsNullOrEmpty(email) ? null : await _users.ValidateCredentialsAsync(email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "That password wasn't correct — your account has not been deleted.");
                return View(model);
            }

            await _users.DeleteAccountAsync(user.Id);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["AccountDeleted"] = true;
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };
            if (user.IsAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}