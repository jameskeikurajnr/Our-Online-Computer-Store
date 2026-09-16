# Online Computer Store — Technical Documentation

This document is the technical companion to `README.md`. Where the README tells you how to
open and run the project, this document explains how it's built — architecture, data model,
security model, and the reasoning behind the key design decisions. It's written to double as
an appendix for the ITEC322 final report.

## 1. Architecture overview

The application is an ASP.NET Core 8 MVC project following a conventional layered
architecture:

```
Controllers/   → HTTP entry points; thin — validate input, call a service, pick a view
Services/      → business logic (cart, orders, users, email, payments, AI)
Models/        → EF Core entities and view models
Components/    → ViewComponents (CartBadge, WishlistBadge, UserAvatar) — small pieces of
                 UI that need their own data lookup on every page render
Data/          → StoreDbContext (EF Core) and DbInitializer (seed + startup bootstrap)
Migrations/    → EF Core code-first schema history
Views/         → Razor views, one folder per controller
wwwroot/       → static assets (CSS, JS, images, vendored libraries)
```

Dependency injection is wired centrally in `Program.cs`: every service is registered against
an interface (`ICartService`/`CartService`, `IOrderService`/`OrderService`,
`IPaymentService`/`StripePaymentService`, etc.), so controllers depend only on abstractions
and the AI/payment/email integrations can be swapped or mocked without touching controller
code.

### Request flow (checkout, the most involved path)

1. `CheckoutController.Index` (GET) shows the checkout form, with `ViewBag.PaymentEnabled`
   telling the view whether to say "Pay with card" or "Place order", and a Ship/Pickup choice
   (Click & Collect — see §5).
2. `CheckoutController.Index` (POST) first requires a **one-time email verification code**
   (see §3, "Checkout email verification") before it will proceed, then branches on
   `IPaymentService.IsConfigured`:
   - **Not configured** → places the order immediately via `IOrderService.PlaceOrderAsync`,
     sends a branded HTML confirmation email, redirects to `Confirmation`. This is the
     graceful-degradation path used when no Stripe keys are set.
   - **Configured** → stashes a `PendingCheckout` snapshot (customer details, fulfillment
     choice, cart lines) in `HttpContext.Session`, creates a Stripe Checkout Session via
     `IPaymentService`, and redirects the browser to Stripe's hosted payment page.
3. Stripe redirects back to `CheckoutController.Success(session_id)` on success (or
   `Cancelled` if the customer backs out). `Success` re-verifies the payment **server-side**
   by calling `GetSessionStatusAsync` against Stripe's API — the client-supplied `session_id`
   is never trusted on its own. It then checks `GetByPaymentReferenceAsync` so refreshing the
   success page can't create a duplicate order, rebuilds the cart from the session snapshot,
   and calls `PlaceOrderAsync` with `paymentProvider: "Stripe"`, `paymentStatus: "Paid"`.
4. `PlaceOrderAsync` also checks each ordered product's resulting stock level and, for any
   that cross the low-stock threshold, emails the store's own inbox (see §5).

## 2. Data model

| Entity | Key fields | Notes |
|---|---|---|
| `Product` | Name, Brand, Price, Category, ImageUrl, IsFeatured, StockQuantity | Seeded by `DbInitializer`; 33 catalog items as of Phase 14 (12 of them Laptops) |
| `AppUser` | Email (unique), PasswordHash/Salt, IsAdmin, ProfilePhotoUrl | Cookie auth principal source; `ProfilePhotoUrl` is a `data:` URL, not a file path (see §6) |
| `Order` | Email, CustomerName, Address, Total, Status, PaymentProvider, PaymentStatus, PaymentReference, FulfillmentMethod, PickupLocation | `PaymentReference` stores the Stripe session ID for idempotency; `FulfillmentMethod` is `"Ship"` or `"Pickup"` |
| `OrderItem` | OrderId (FK), ProductId, ProductName, UnitPrice, Quantity | One-to-many with `Order`, cascade delete |
| `SavedCartItem` | UserId, ProductId, Quantity | Backs the account-linked persistent cart (§5) |
| `ProductReview` | ProductId, UserId, UserName, Rating, Comment, CreatedAt | One review per user per product (unique index) |
| `WishlistItem` | UserId, ProductId, AddedAt | One entry per user per product (unique index) |
| `PasswordResetToken` | UserId, Token (unique), ExpiresAt, Used | Single-use, 1-hour expiry, backs the forgot-password flow |
| `AuditLogEntry` | AdminUserId, AdminName, Action, Details, CreatedAt | Every admin action; see §5 for the "Clear All" design |
| `PageView` | Path, CreatedAt | One row per real page request; see §12 |

Schema changes are managed through EF Core code-first migrations, applied automatically at
startup via `context.Database.Migrate()` in `DbInitializer.Initialize()` — there's no manual
migration step for whoever runs the app. Migrations added on top of the initial schema:

| Migration | Adds |
|---|---|
| `AddPaymentFieldsToOrder` | `PaymentProvider`, `PaymentStatus`, `PaymentReference` on `Order` |
| `AddIsAdminToUser` | `IsAdmin` on `AppUser` |
| `AddSavedCartItems` | `SavedCartItems` table (account-linked persistent cart) |
| `AddStoreImprovements` | `StockQuantity` on `Product`; `ProductReviews`, `WishlistItems`, `PasswordResetTokens`, `AuditLogEntries` tables |
| `AddClickAndCollect` | `FulfillmentMethod`, `PickupLocation` on `Order` |
| `AddProfilePhotoToUser` | `ProfilePhotoUrl` on `AppUser` |
| `AddPageViews` | `PageViews` table (`Id`, `Path`, `CreatedAt`) — see §12 |

## 3. Security model

### Authentication
Cookie-based authentication (`Microsoft.AspNetCore.Authentication.Cookies`) with a
`ClaimsPrincipal` built from the user's row in `Users`. Passwords are hashed with PBKDF2
(`Rfc2898DeriveBytes.Pbkdf2`, 100,000 iterations, SHA-256, 32-byte output) — not reversible,
not a fast hash, salted per user.

### Authorization — admin lockdown
`AdminController` (product management, orders, dashboard, audit log) is decorated with
`[Authorize(Roles = "Admin")]`. The `Admin` role claim is only added to a user's
`ClaimsIdentity` when their `AppUser.IsAdmin` flag is `true`. Nobody starts as admin except by
a one-time bootstrap: on every startup, `DbInitializer` checks whether *any* user has
`IsAdmin = true`, and if not, promotes whichever account was registered first. This means a
fresh database always ends up with exactly one admin (the first person to register) with no
manual database editing required, while leaving a documented, findable way to re-target it
(flip the column directly) if needed.

### Admin action audit log
Every admin action — stock changes, order status updates, AI description regeneration, CSV
exports, and the audit log's own clearing — is recorded via `IAuditLogService.LogAsync`
(admin id, name, action, free-text details, timestamp). The Audit Log page has a "Clear All"
button (confirmation required — it's destructive across the whole log), which wipes every row
and then immediately writes one fresh entry recording that the log was cleared and by whom, so
resetting the log never loses the trace that it happened. Business-transactional data — Orders
and Products — deliberately has no "Clear All": those are real records, not accumulating
clutter, so bulk deletion isn't exposed in the UI for them. Wishlist gets a lighter-weight
"Clear Wishlist" (no confirmation, mirroring the existing "Clear Cart" pattern) since it's
personal, easily-undone data, not shared admin history.

### Order-tracking IDOR fix
Order IDs are short, sequential-feeling 5-digit numbers — easy to guess or enumerate.
Originally, `/Orders/Track` looked an order up by ID alone, meaning anyone could page through
IDs and read other customers' names, addresses, and order contents. The fix requires the
email address used at checkout to match the order before any details are shown, and returns
one generic error ("We couldn't find an order with that ID and email combination") whether
the ID doesn't exist or the email doesn't match — so the error itself can't be used to
enumerate valid order IDs.

### Checkout email verification (one-time code)
Before an order can be placed — regardless of payment method — the checkout flow requires a
one-time code emailed to the address the customer provided, entered back into the checkout
form. This adds a second factor to checkout itself (proof of access to the stated email) on
top of Stripe's own payment verification, and is generated/validated independently of the
password-reset token system.

### Login rate limiting
The login endpoint is decorated with ASP.NET Core's built-in rate limiter (`AddRateLimiter`,
policy `"login"`): 5 attempts per 5-minute window, keyed by client IP. A client that exceeds
this gets an immediate `429 Too Many Requests` rather than continuing to hammer the password
check, blunting brute-force guessing without needing a CAPTCHA or account-lockout UX.

### Payment security
- Stripe secret key never reaches the browser; only the publishable key would (and even that
  isn't currently exposed client-side, since Checkout is fully redirect-based).
- Payment status is written to the database only after the server calls Stripe's API and
  confirms the session's `payment_status` is `paid` — the success redirect URL is not treated
  as proof of payment on its own.

### Secrets management
Real credentials (SMTP password, OpenAI API key, Stripe secret key) are kept out of
`appsettings.json` in two layers: for local development, `dotnet user-secrets` (the project
already carries a `UserSecretsId` in its `.csproj`) stores them outside the project folder
entirely, picked up automatically whenever the app runs in the `Development` environment (the
default for `F5` — see `Properties/launchSettings.json`); for anything deployed beyond local
dev, the same settings are read from environment variables instead, using ASP.NET Core's
built-in `Section__Key` convention (`Smtp__Password`, `Ai__ApiKey`, `Stripe__SecretKey`) —
environment variables take precedence over `appsettings.json` automatically, no code change
required either way. `appsettings.json` itself is git-ignored; `appsettings.example.json` is
the committed placeholder template new setups copy from.

## 4. Payments (Stripe) — implementation notes

Stripe is called via a raw, typed `HttpClient` (`StripePaymentService : IPaymentService`)
rather than the Stripe.NET SDK, form-encoding requests to
`POST https://api.stripe.com/v1/checkout/sessions` with a Bearer-token secret key, and
`GET .../checkout/sessions/{id}` to verify status afterward. Cart line items are converted to
Stripe's `line_items[i][price_data]...` form fields, with prices converted to integer cents
(`Math.Round(price * 100m, MidpointRounding.AwayFromZero)`) since Stripe's API works in the
smallest currency unit. `IPaymentService.IsConfigured` gates the whole flow, so the app runs
identically well with Stripe fully wired up or with no keys at all — useful for demos,
grading, or environments without live keys.

## 5. Store operations & fulfillment

### Click & Collect
Checkout offers a Ship/Pickup choice (`Order.FulfillmentMethod`). Pickup orders record which
of the two physical locations (Darling Harbour or Parramatta — shown on the Contact page,
with address/phone/hours for each) the customer chose (`Order.PickupLocation`); the order
confirmation email and order-tracking page reflect the chosen method and, for pickup, the
location.

### Admin stock restock UI
`Admin > Products` has an inline control (`AdminController.UpdateStock`) to set a product's
exact stock total — how an admin replenishes an out-of-stock item or corrects a count after a
stocktake. The change is written straight to `Product.StockQuantity` and logged to the audit
trail with the before/after quantities.

### Low-stock email alerts
When `OrderService.PlaceOrderAsync` completes, it checks whether any ordered product's
resulting stock crossed at/below a low-stock threshold and, if so, emails the store's own
inbox (`IEmailService.SendLowStockAlertAsync`) listing the affected products and their
remaining quantity — so restocking doesn't depend on someone remembering to check the catalog.

### "Customers also bought"
Product detail pages show real co-purchase suggestions
(`IOrderService.GetFrequentlyBoughtTogetherAsync`) computed from what other customers actually
ordered alongside the current product — a stronger signal than the AI-generated upsell text
shown alongside it once there's enough order history.

### Orders CSV export
`Admin > Orders` can download the (optionally status-filtered) order list as a CSV
(`AdminController.ExportOrdersCsv`) — order id, customer, items, total, payment/fulfillment
details, status, and placed-at timestamp — for pulling sales data into Excel/Sheets without
touching the database directly. The export itself is audit-logged.

## 6. Customer experience features

### Account-linked persistent cart
Guests get a session-based cart; signing in or registering merges that guest cart into the
account's own saved cart (`ICartService.MergeGuestCartIntoAccount`), and logging out snapshots
the account cart back into the session (`SnapshotAccountCartToSession`) so the browser keeps
showing the same cart immediately after logout rather than appearing empty, while the saved
copy under the account stays intact for next time — even from a different browser.

### Wishlist
Account-only; toggle a product in/out from the shop or product page, with a "Clear Wishlist"
button once there's anything in it (mirroring "Clear Cart").

### Product reviews
One star rating + comment per user per product, shown on the product detail page alongside an
average rating and review count.

### Trending This Week
Home and Shop both show a "Trending This Week" shelf, ranked by real order data —
`ProductService.GetTrendingAsync()` sums units sold per product over the last 7 days and
returns every product in that order (ties broken by featured status, then in-stock, then id,
so the ranking is fully deterministic). Home takes the top 2; Shop takes the next 7 from the
same ranking (skipping Home's 2), so the two pages never show the same item. Before a week of
real order history has built up, the fallback ordering (featured, then in-stock) keeps the
shelf populated instead of empty.

### Profile photos
Accounts can upload a profile photo from Edit Profile (JPEG/PNG/GIF/WEBP, capped at 1 MB).
It's stored as a base64 `data:` URL directly on the `AppUser` row rather than as a file on
disk — deliberately, so there's nothing to orphan on the filesystem when an account is
deleted, and no writable-directory assumption for wherever the app happens to be hosted. When
unset, a teal initials circle is shown instead (`UserAvatarViewComponent`, rendered fresh per
request rather than carried in the auth cookie, since even a small image is far too big for a
cookie claim). Clicking any photo — navbar, Manage Account, Edit Profile — opens it full-size
in a shared lightbox overlay (`site.js`/`site.css`), closable via backdrop click, the × button,
or Escape.

### Order confirmation email
Sent as `multipart/alternative`: a short, branded HTML version (navy header with the teal
"OCS" mark, itemized order summary, total, pickup/shipping note) as the primary body, with the
original plain-text version kept as the fallback for clients that don't render HTML. Customer-
supplied fields (name, address, pickup location) are HTML-escaped before being placed in the
markup.

## 7. AI-assisted features

Several optional features call an OpenAI-compatible chat completions endpoint (product
search, homepage copy, product comparisons, upsell suggestions, order-status explanations),
each behind its own small service implementing a narrow interface (`IAiSearchService`,
`IAiOrderExplainService`, etc.), all built on a shared `AiServiceBase`. When `Ai:Enabled` is
`false` or no key is set, each feature responds with a clear "AI not configured yet" message
instead of failing — the UI never breaks, it just quietly degrades.

## 8. Known simplifications / deliberate scope cuts

- Custom cookie auth rather than full ASP.NET Core Identity, to keep the auth code readable
  for a capstone-sized project.
- No refund/webhook handling on the Stripe integration — the `WebhookSecret` config slot
  exists for future use but isn't wired to an endpoint yet.
- Automated test coverage is partial: `OnlineComputerStore.Tests` covers `OrderService`
  (including the low-stock alert behavior, via a hand-written `FakeEmailService` test double),
  but `CartService`, `UserService`, and `StripePaymentService` are not yet under test.
- A separate, legacy ASP.NET Web Forms (.NET Framework) prototype of this project exists in a
  different folder (`1. WEBSITE Online Computer Store`) — an earlier UI sketch, not part of
  this ASP.NET Core solution. See §10 for what it actually contains and the decision on it.

## 9. Deployment notes

The app is currently designed for local/dev use (SQLite by default, `dotnet run` /
Visual Studio F5). For a real deployment: switch `Database:Provider` to `SqlServer` and point
`ConnectionStrings:SqlServer` at a real instance; supply `Smtp`, `Ai`, and `Stripe` secrets via
environment variables (`Section__Key` naming — see §3) rather than `appsettings.json`, since
`dotnet user-secrets` only applies in the `Development` environment; set Stripe to live keys
only after a deliberate go-live decision, since test keys are what keep the current checkout
risk-free; and add HTTPS enforcement/HSTS if not already active behind a reverse proxy.

## 10. QA verification (WBS Phase 6) & legacy remediation (WBS Phase 7)

### Admin access control — verified
`AdminController` carries a single class-level `[Authorize(Roles = "Admin")]`, so it applies to
every action on it (`Products`, `GenerateDescription`, `UpdateStock`, `Orders`,
`UpdateOrderStatus`, `Dashboard`, `AuditLog`, `ClearAuditLog`, `ExportOrdersCsv`) — there's no
per-action gate to accidentally miss. A pass over every other controller confirms nothing
admin-only leaked into them: `WishlistController` and the relevant `ShopController` action
require `[Authorize]` (signed-in, not necessarily admin, which is correct for those), and
`AccountController`'s authenticated-only actions (manage account, edit profile, change
password, delete account, logout) are individually `[Authorize]`, while registration,
login, and forgot-password are deliberately open to anonymous visitors.

### Order-tracking email check — verified
`OrdersController.Track` (POST) rejects a request with no email, then requires the submitted
email to case-insensitively match `Order.Email` before returning any order details, and
returns the same generic "couldn't find an order" message whether the order ID doesn't exist
or the email doesn't match — so the error response itself can't be used to enumerate valid
order IDs. Confirmed by reading the current controller code line-by-line, not just from memory
of the original fix.

### Cross-browser / responsive pass
This was done as a static/code-level review — inspecting layout CSS and markup rather than
opening the running site in physical browsers, since that isn't available from here. Confirmed
present and correct: a `viewport` meta tag; Bootstrap's standard `navbar-expand-lg` collapse
pattern for the mobile menu; `row-cols-*`/`col-md-*` grid classes throughout the product,
shop, and trending grids, which stack to one column below their breakpoint. One real bug and
two rough edges were found and fixed:
- **Assistant widget overflow (bug):** `#assistant-panel` was a fixed `320px` wide, pinned
  `right: 0` against a button offset `24px` from the screen edge — on a phone narrower than
  about 370px, the panel ran off the left side of the viewport. Fixed with a `max-width: 400px`
  media query that sizes the panel to `calc(100vw - 24px)` instead.
- **Navbar search width:** `.ai-search` capped at `max-width: 240px` for the desktop layout
  looked cramped once the menu collapses to a full-width mobile stack. Fixed with a media query
  that lets it use the full row width below the `lg` breakpoint.
- **Hero heading size:** `.hero h1` was a fixed `2.6rem`; changed to `clamp(1.8rem, 5vw + 1rem,
  2.6rem)` so it scales down on narrow screens instead of forcing extra height.

Given these were verified by reading code and CSS rather than driving a live browser, a final
manual click-through (phone width + desktop, in whichever browsers matter for grading) before
submission is still worth the ten minutes, but the known, fixable issues found this way have
been fixed.

### Legacy Web Forms prototype — decision and remediation
The separate `1. WEBSITE Online Computer Store` folder is an ASP.NET Web Forms (.NET
Framework 4.8) sketch built before this project, not part of this solution. Reading its actual
code (rather than relying on an earlier assumption) shows it contains no fake card-collection
form — `Checkout.aspx` only ever asks for name/address/phone and explicitly tells the visitor
"Payment isn't collected online here." What it does contain: a hardcoded `admin`/`password`
login, a "registration" that doesn't create an account, a "track order" that only ever matches
one hardcoded ID (`ABC123`), and a `Web.config` with a placeholder (not real) SMTP password.
None of that is a security hole to patch — it's a throwaway UI prototype that was never meant
to hold real logic, and patching it to "work" would just be extra effort spent on a codebase
this project doesn't use.

**Decision:** retire it, don't repair it. **Remediation implemented:** a `README.md` was added
at the root of that folder, spelling out exactly what's fake in it and recommending it be kept
out of the Git repository pushed for this project (or, if kept for history, pushed only to a
clearly separate, clearly labeled archive repo) — so nobody, including a future reader of this
report, mistakes it for working functionality.

## 11. Storefront UX modernization (WBS Phase 14)

A visual-design pass across the pages that hadn't been touched since the original build,
plus a real fix for a UX gap and a catalog expansion. Full task-by-task detail is in
`clickup_import_wbs.csv` (WBS 14.1–14.9); this section covers the parts worth understanding
as implementation, not just as a changelog entry.

### Shop category filtering
`ShopController.Index` gained an optional `category` query parameter, matched
case-insensitively against `Product.Category`. The filter is applied before sorting and
paging, and threaded through every link that needs to preserve it: the sort form (a hidden
`category` input), and all three pagination links (`Previous`/page-numbers/`Next`, via
`asp-route-category`). When a category filter is active, the "Trending" shelf re-ranks within
just that category instead of mixing in items the page is intentionally hiding. This is what
Home's "Shop Laptops" button now links to (`asp-route-category="Laptops"`) — previously it and
"Explore Our Products" were functionally identical.

### Track Order: Order Summary
`Order.Items` (the order's line items) and `Order.Total` existed on the model but weren't
rendered anywhere on the customer-facing order-tracking result page. `TrackResult.cshtml` now
shows an itemized summary (product name, quantity, line total) and the order total, using data
the app already had — not a new query or migration, just a previously-unused part of the
existing `Order` object finally reaching the view.

### Placeholder-image system for unphotographed products
Products without real photography (currently the 6 laptops added this sprint) get
`ImageUrl` pointed at a locally-generated placeholder JPEG rather than a blank string — a
blank `ImageUrl` would break the `<img>` tag on every customer-facing page that lacks the
Admin page's fallback-icon logic, whereas a placeholder image is a normal, always-loadable
asset that requires zero template changes anywhere in the codebase. Each placeholder is
brand-colored (an accent bar/icon tint plus a plain-text brand name — no logos, so there's no
trademark reproduction) rather than one shared generic graphic, so a product grid with several
unphotographed items still reads as distinct listings. Swapping in a real photo later is a
one-line `ImageUrl` change in `DbInitializer.cs` (plus, for products already seeded into a
live database, a direct `UPDATE` — see the next paragraph — since the seeder is additive-only
and won't retroactively touch existing rows).

### `DbInitializer` is additive-only — know this before editing seed data
`DbInitializer.Initialize()` runs on every startup, dedupes by `Product.Name`, and
`AddRange`s only the names missing from the database. This is safe and restart-proof for
*adding* new products, but it means **editing an existing seed entry's fields (price, image,
description, etc.) has no effect on a database that already has that row** — the row already
exists, so it's never re-added, and the seeder never issues an `UPDATE`. Phase 14's placeholder
-image fix needed both: `DbInitializer.cs` updated (correct for any future/fresh database) and
a direct SQL `UPDATE` against the live `onlinecomputerstore.db` for the 6 rows that already
existed. Anyone changing seed data for products that might already be seeded elsewhere should
check the live database, not just the seed file.

### Design pattern used for this pass
Every page in this phase went through the same sequence: build a static HTML mockup reusing
the site's real CSS custom properties (`--brand-teal`, `--card-bg`, `--text-primary`, etc. —
never hand-picked colors) → render light/dark/mobile screenshots with Playwright → get
approval → make the real `.cshtml`/`.cs` change, preserving every existing model binding and
`asp-*` attribute exactly → re-render the *actual* shipped file's markup (not the mockup) with
Playwright and check for console errors → push. This caught at least one real bug before it
shipped (the `text-danger`/custom-color conflict on `Track.cshtml`, WBS 14.6) that wasn't
visible in the mockup itself.

## 12. Site traffic tracking (PageView)

`PageViewMiddleware` (registered in `Program.cs` after `UseRouting`, so it can see the real
final status code) logs one `PageView` row per real page request — a `GET` that actually
returned `200 OK`, excluding `/Admin/*` (so an admin loading their own dashboard doesn't
inflate the number that same dashboard shows them), `/api/*` calls, and anything file-like.
It deliberately records nothing that identifies a visitor — no IP address, no cookie or
session id, no user agent — just the path and the time, which is enough to count visits per
day. This feeds the Admin Dashboard's "Traffic" line, which previously showed hardcoded sample
data. A save failure inside the middleware is swallowed intentionally: logging traffic should
never be able to break the page it's counting.

This landed in the same working copy as the Phase 14 catch-up commit but wasn't captured as its
own WBS task at build time — its origin wasn't immediately obvious from `git status` alone, and
tracing it surfaced a real risk worth noting: `Program.cs` and `StoreDbContext.cs` already
referenced `PageViewMiddleware`/`PageView` by the time this was found, so the feature had to be
committed to keep the repository compiling on a fresh clone, not just as a nice-to-have. Added
to `clickup_import_wbs.csv` retroactively as WBS 14.10–14.11 once confirmed. See `SPRINT.md`
for the full account.
