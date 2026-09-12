using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineComputerStore.Core.Models;
using OnlineComputerStore.Core.Services;

namespace OnlineComputerStore.Core.Controllers
{
    public class CheckoutController : Controller
    {
        private const string PendingCheckoutKey = "PendingCheckout";
        private const string OtpStateKey = "CheckoutOtp";
        private const string VerifiedEmailKey = "CheckoutVerifiedEmail";
        private const string VerifiedAtKey = "CheckoutVerifiedAt";

        // How long a completed verification stays good for before checkout would
        // ask again, how long an emailed code itself stays valid, how many wrong
        // guesses are allowed before a fresh code is required, and the minimum
        // gap between "resend" requests.
        private const int VerificationValidMinutes = 30;
        private const int CodeValidMinutes = 10;
        private const int MaxAttempts = 5;
        private const int ResendCooldownSeconds = 60;

        // Click & Collect locations — mirrors the two stores listed on the Contact page.
        private static readonly string[] PickupLocations = { "Darling Harbour", "Parramatta" };

        private readonly ICartService _cart;
        private readonly IOrderService _orders;
        private readonly IEmailService _email;
        private readonly IPaymentService _payments;

        public CheckoutController(ICartService cart, IOrderService orders, IEmailService email, IPaymentService payments)
        {
            _cart = cart;
            _orders = orders;
            _email = email;
            _payments = payments;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            // Every checkout — guest or signed in — has to prove ownership of the
            // email an order will be placed under before it sees the checkout form.
            var verifiedEmail = GetVerifiedEmail();
            if (verifiedEmail == null)
                return RedirectToAction("Verify");

            ViewBag.PaymentEnabled = _payments.IsConfigured;
            return View(new CheckoutViewModel { Email = verifiedEmail });
        }

        [HttpPost]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                return View(model);
            }

            // The Email field on the form is locked to the verified address, but
            // guard against it being tampered with anyway — if it doesn't match,
            // treat this as unverified and send them back through the code flow.
            var verifiedEmail = GetVerifiedEmail();
            if (verifiedEmail == null || !string.Equals(verifiedEmail, model.Email, StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Verify");

            // Address and PickupLocation are only conditionally required, depending on
            // FulfillmentMethod — not something a plain [Required] attribute can express.
            if (model.FulfillmentMethod == "Pickup")
            {
                ModelState.Remove(nameof(model.Address));
                if (!PickupLocations.Contains(model.PickupLocation))
                    ModelState.AddModelError(nameof(model.PickupLocation), "Please choose a pickup location.");
                else
                    // Keep Address populated with a human-readable summary so every other
                    // place that displays an order (emails, tracking, admin) needs no
                    // special-casing for pickup orders.
                    model.Address = $"Pickup — {model.PickupLocation}";
            }
            else
            {
                model.FulfillmentMethod = "Ship";
                model.PickupLocation = null;
                ModelState.Remove(nameof(model.PickupLocation));
                if (string.IsNullOrWhiteSpace(model.Address))
                    ModelState.AddModelError(nameof(model.Address), "The Shipping address field is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentEnabled = _payments.IsConfigured;
                return View(model);
            }

            // Stripe isn't set up (Ai/Smtp-style graceful degradation) — place the
            // order the same way the site always has, with no payment step.
            if (!_payments.IsConfigured)
            {
                var order = await _orders.PlaceOrderAsync(model, cart);
                _cart.Clear();
                ClearVerification();
                await _email.SendOrderConfirmationAsync(order);
                return RedirectToAction("Confirmation", new { id = order.Id });
            }

            // Stash the checkout details + a price/name snapshot of the cart server-side
            // (in Session) so the order can be built from exactly what the customer paid
            // for, once Stripe confirms the payment — the order isn't created yet.
            var pending = new PendingCheckout
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Address = model.Address,
                FulfillmentMethod = model.FulfillmentMethod,
                PickupLocation = model.PickupLocation,
                Lines = cart.Items.Select(i => new PendingCheckoutLine
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity
                }).ToList()
            };
            HttpContext.Session.SetString(PendingCheckoutKey, JsonSerializer.Serialize(pending));

            var successUrl = Url.Action("Success", "Checkout", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action("Cancelled", "Checkout", null, Request.Scheme)!;

            var session = await _payments.CreateCheckoutSessionAsync(cart, model.Email, successUrl, cancelUrl);
            if (!session.Success || string.IsNullOrWhiteSpace(session.Url))
            {
                ModelState.AddModelError("", session.Error ?? "Couldn't start checkout with the payment provider. Please try again.");
                ViewBag.PaymentEnabled = _payments.IsConfigured;
                return View(model);
            }

            // Off to Stripe's own hosted payment page — this app never sees card details.
            return Redirect(session.Url);
        }

        // Stripe sends the customer back here after a successful payment.
        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return RedirectToAction("Index");

            // If this session_id already has an order (e.g. the customer refreshed this
            // page, or came back to it), don't place a second one — just show it again.
            var existingOrder = await _orders.GetByPaymentReferenceAsync(session_id);
            if (existingOrder != null)
                return RedirectToAction("Confirmation", new { id = existingOrder.Id });

            // Never trust the redirect alone — ask Stripe directly whether this specific
            // session actually completed payment before creating an order for it.
            var status = await _payments.GetSessionStatusAsync(session_id);
            if (!status.Found || !status.IsPaid)
            {
                ViewBag.Message = status.Error ?? "We couldn't confirm that payment went through. If you were charged, please contact us with your Stripe reference so we can look into it.";
                ViewBag.SessionId = session_id;
                return View("PaymentIncomplete");
            }

            var json = HttpContext.Session.GetString(PendingCheckoutKey);
            if (string.IsNullOrEmpty(json))
            {
                // Payment succeeded but we lost the matching session-side checkout details
                // (e.g. the browser session expired or this is a different device/browser).
                ViewBag.Message = "Payment succeeded, but we couldn't match it to your checkout details on this device. Please contact us with the reference below and we'll sort it out.";
                ViewBag.SessionId = session_id;
                return View("PaymentIncomplete");
            }

            var pending = JsonSerializer.Deserialize<PendingCheckout>(json);
            if (pending == null || pending.Lines.Count == 0)
            {
                ViewBag.Message = "Payment succeeded, but your checkout details looked incomplete. Please contact us with the reference below.";
                ViewBag.SessionId = session_id;
                return View("PaymentIncomplete");
            }

            var model = new CheckoutViewModel
            {
                CustomerName = pending.CustomerName,
                Email = pending.Email,
                Address = pending.Address,
                FulfillmentMethod = pending.FulfillmentMethod,
                PickupLocation = pending.PickupLocation
            };

            var cart = new Cart();
            foreach (var line in pending.Lines)
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    // Rebuilt from the price/name snapshot taken at checkout time — this is
                    // what the customer actually paid, regardless of any later price change.
                    Product = new Product { Name = line.ProductName, Price = line.UnitPrice }
                });
            }

            var order = await _orders.PlaceOrderAsync(model, cart, paymentProvider: "Stripe", paymentStatus: "Paid", paymentReference: session_id);

            _cart.Clear();
            ClearVerification();
            HttpContext.Session.Remove(PendingCheckoutKey);
            await _email.SendOrderConfirmationAsync(order);

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // Stripe sends the customer back here if they back out of the hosted payment page.
        [HttpGet]
        public IActionResult Cancelled()
        {
            // Cart and pending checkout details are left untouched so they can just try again.
            return View();
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        // Entry point for the verification gate. GET here (rather than straight to the
        // checkout form) whenever Index() finds no valid verification on this session.
        [HttpGet]
        public async Task<IActionResult> Verify()
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            // Already verified within the last VerificationValidMinutes — go straight to checkout.
            if (GetVerifiedEmail() != null)
                return RedirectToAction("Index");

            if (!_email.IsConfigured)
                return View("VerifyUnavailable");

            // Signed-in customers verify their account email automatically rather than retyping it.
            var accountEmail = CurrentAccountEmail();
            if (!string.IsNullOrWhiteSpace(accountEmail))
            {
                var state = GetOtpState();
                if (state == null || !string.Equals(state.Email, accountEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var (sent, error) = await IssueCodeAsync(accountEmail);
                    if (!sent)
                    {
                        ViewBag.Message = error ?? "We couldn't send the verification code just now. Please try again in a moment.";
                        return View("VerifyUnavailable");
                    }
                }

                ViewBag.Email = accountEmail;
                return View("VerifyCode", new VerifyCodeViewModel());
            }

            return View("VerifyEmail", new VerifyEmailViewModel());
        }

        // Guest step 1: they type the email an OTP should be sent to.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            if (!_email.IsConfigured)
                return View("VerifyUnavailable");

            if (!ModelState.IsValid)
                return View("VerifyEmail", model);

            var (success, error) = await IssueCodeAsync(model.Email);
            if (!success)
            {
                ModelState.AddModelError("", error ?? "We couldn't send the verification code just now. Please try again in a moment.");
                return View("VerifyEmail", model);
            }

            ViewBag.Email = model.Email;
            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // Step 2 for everyone: they type the 6-digit code that was emailed to them.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            var state = GetOtpState();
            if (state == null)
                return RedirectToAction("Verify");

            ViewBag.Email = state.Email;

            if (!ModelState.IsValid)
                return View("VerifyCode", model);

            if (DateTime.UtcNow > state.ExpiresAt)
            {
                HttpContext.Session.Remove(OtpStateKey);
                var (resent, resendError) = await IssueCodeAsync(state.Email);
                ModelState.AddModelError("", "That code expired, so we've sent you a fresh one — please check your email.");
                if (!resent)
                    ModelState.AddModelError("", resendError ?? "We couldn't send a new code just now. Please try again in a moment.");
                ViewBag.Email = state.Email;
                return View("VerifyCode", new VerifyCodeViewModel());
            }

            if (!string.Equals(model.Code, state.Code, StringComparison.Ordinal))
            {
                state.Attempts++;
                if (state.Attempts >= MaxAttempts)
                {
                    HttpContext.Session.Remove(OtpStateKey);
                    var (resent, resendError) = await IssueCodeAsync(state.Email);
                    ModelState.AddModelError("", "Too many incorrect attempts, so we've sent you a fresh code — please check your email.");
                    if (!resent)
                        ModelState.AddModelError("", resendError ?? "We couldn't send a new code just now. Please try again in a moment.");
                    ViewBag.Email = state.Email;
                    return View("VerifyCode", new VerifyCodeViewModel());
                }

                SaveOtpState(state);
                ModelState.AddModelError("", $"That code isn't right. You have {MaxAttempts - state.Attempts} attempt(s) left.");
                return View("VerifyCode", new VerifyCodeViewModel());
            }

            // Correct code — mark this email verified for checkout and drop the OTP state.
            HttpContext.Session.Remove(OtpStateKey);
            SetVerified(state.Email);

            return RedirectToAction("Index");
        }

        // "Resend code" link on the VerifyCode view.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode()
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any())
                return RedirectToAction("Index", "Cart");

            var state = GetOtpState();
            if (state == null)
                return RedirectToAction("Verify");

            ViewBag.Email = state.Email;

            if (DateTime.UtcNow < state.LastSentAt.AddSeconds(ResendCooldownSeconds))
            {
                ModelState.AddModelError("", "Please wait a little before requesting another code.");
                return View("VerifyCode", new VerifyCodeViewModel());
            }

            var (success, error) = await IssueCodeAsync(state.Email);
            if (!success)
                ModelState.AddModelError("", error ?? "We couldn't send a new code just now. Please try again in a moment.");
            else
                ViewBag.StatusMessage = "We've sent a new code to your email.";

            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // --- Verification session-state helpers -----------------------------------------

        private string? GetVerifiedEmail()
        {
            var email = HttpContext.Session.GetString(VerifiedEmailKey);
            if (string.IsNullOrEmpty(email)) return null;

            var atString = HttpContext.Session.GetString(VerifiedAtKey);
            if (!DateTime.TryParse(atString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var verifiedAt) ||
                DateTime.UtcNow > verifiedAt.AddMinutes(VerificationValidMinutes))
            {
                ClearVerification();
                return null;
            }

            return email;
        }

        private void SetVerified(string email)
        {
            HttpContext.Session.SetString(VerifiedEmailKey, email);
            HttpContext.Session.SetString(VerifiedAtKey, DateTime.UtcNow.ToString("O"));
        }

        private void ClearVerification()
        {
            HttpContext.Session.Remove(VerifiedEmailKey);
            HttpContext.Session.Remove(VerifiedAtKey);
        }

        private CheckoutOtp? GetOtpState()
        {
            var json = HttpContext.Session.GetString(OtpStateKey);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonSerializer.Deserialize<CheckoutOtp>(json);
        }

        private void SaveOtpState(CheckoutOtp state)
        {
            HttpContext.Session.SetString(OtpStateKey, JsonSerializer.Serialize(state));
        }

        private string? CurrentAccountEmail()
        {
            if (User?.Identity?.IsAuthenticated == true)
                return User.FindFirstValue(ClaimTypes.Email);
            return null;
        }

        private static string GenerateCode()
        {
            // Cryptographically random, not System.Random — this gates a real security check.
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }

        private async Task<(bool Success, string? Error)> IssueCodeAsync(string email)
        {
            var code = GenerateCode();
            var (success, message) = await _email.SendCheckoutVerificationCodeAsync(email, code);
            if (!success)
                return (false, message);

            SaveOtpState(new CheckoutOtp
            {
                Email = email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CodeValidMinutes),
                LastSentAt = DateTime.UtcNow,
                Attempts = 0
            });

            return (true, null);
        }

        private class CheckoutOtp
        {
            public string Email { get; set; } = "";
            public string Code { get; set; } = "";
            public DateTime ExpiresAt { get; set; }
            public DateTime LastSentAt { get; set; }
            public int Attempts { get; set; }
        }

        private class PendingCheckout
        {
            public string CustomerName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Address { get; set; } = "";
            public string FulfillmentMethod { get; set; } = "Ship";
            public string? PickupLocation { get; set; }
            public List<PendingCheckoutLine> Lines { get; set; } = new();
        }

        private class PendingCheckoutLine
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
        }
    }
}
