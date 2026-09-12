# Online Computer Store — ASP.NET Core 8 MVC

A complete, runnable rebuild of your Online Computer Store as a modern ASP.NET Core 8 MVC
web application, with an optional AI layer (product search, an on-site assistant, AI-written
product descriptions, AI order explanations, AI product comparisons, and AI homepage/upsell
copy) that you can turn on with your own OpenAI API key.

## Features

**Shopping**
- Product catalog with search, sort (name/price/brand), and pagination
- "Trending This Week" — a real, sales-ranked shelf on Home and Shop (never overlapping
  between the two pages), with a sensible fallback before a week of order history exists
- "Customers also bought" on product pages, computed from real past orders
- Wishlist (toggle + "Clear Wishlist"), star ratings & reviews (one per customer per product)
- Cart works for guests; signing in merges it into your account, so it survives logging out
  and back in, even from a different browser

**Checkout & orders**
- Ship or Click & Collect (pick up at Darling Harbour or Parramatta)
- Stripe Checkout in test mode, with a one-time email code required before any order is placed
- Branded HTML order confirmation email (plain-text fallback included)
- Public order tracking by order ID + the email used at checkout
- Low-stock email alerts to the store whenever an order pushes a product to/below threshold

**Accounts**
- Register/login, edit profile, change password, forgot-password email flow
- Profile photos — upload one (JPEG/PNG/GIF/WEBP, up to 1 MB), or get an auto-generated
  initials avatar; click any photo, anywhere on the site, to view it full-size
- Delete account

**Admin** (first registered account is auto-promoted; `/Admin`, role-gated)
- Dashboard (revenue, orders by status, top products)
- Orders list with status updates and CSV export
- Product management, including inline stock restocking and AI description generation
- Audit log of every admin action, with a "Clear All" (confirmed, self-logging)

**AI (optional, your own OpenAI key)**
- Product search, an on-site shopping assistant, AI-written product descriptions, AI order
  explanations, AI product comparisons, AI homepage/upsell copy — all degrade to a clear
  "AI not configured yet" message rather than breaking when no key is set

## Opening the project in Visual Studio 2022

1. Unzip this folder anywhere on your machine.
2. Double-click **`OnlineComputerStore.sln`** — it will open in Visual Studio 2022.
3. Make sure you have the **ASP.NET and web development** workload installed
   (Visual Studio Installer → Modify, if you don't already have it).
4. Press **F5** (or the green ▶ "https" run button). Visual Studio will restore the NuGet
   packages automatically the first time you build.
5. The site will open in your browser at a `https://localhost:xxxx` address. The database
   (SQLite, a single file) is created and seeded automatically the first time the app runs —
   there is nothing else to set up.

## Configuration — `appsettings.json`

This file lives at:

```
OnlineComputerStore.Core/appsettings.json
```

In Visual Studio's **Solution Explorer**, it's the file literally named `appsettings.json`
directly inside the `OnlineComputerStore.Core` project node (not inside a subfolder). It
controls the database and the AI integration.

### Database provider

```json
"Database": { "Provider": "Sqlite" }
```

- **`Sqlite`** (default) — no setup required, stores everything in a single
  `onlinecomputerstore.db` file next to the app. Best for just running the site.
- **`SqlServer`** — set the provider to `"SqlServer"` and adjust the `SqlServer` connection
  string under `ConnectionStrings` if you'd rather use a real SQL Server / LocalDB instance.

### AI features (OpenAI)

```json
"Ai": {
  "Enabled": false,
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "",
  "Model": "gpt-4o-mini"
}
```

To turn the AI features on:

1. Get an API key from https://platform.openai.com/api-keys.
2. Set `"Enabled": true` and paste your key into `"ApiKey"`.
3. (Optional) change `"Model"` to any OpenAI chat model you have access to.

**Recommended:** rather than pasting your key directly into `appsettings.json` (which could
end up committed to source control), use .NET's built-in user-secrets store. In Visual
Studio, right-click the project → **Manage User Secrets**, or from a terminal in the project
folder:

```
dotnet user-secrets set "Ai:Enabled" "true"
dotnet user-secrets set "Ai:ApiKey" "sk-...your key..."
```

Until a key is configured, the AI features stay active in the UI but simply respond with
"AI not configured yet" — this is expected, not a bug.

### Email (SMTP — contact form, order confirmations, password reset, checkout codes)

```json
"Smtp": {
  "Enabled": false,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "",
  "Password": "",
  "FromAddress": "",
  "FromName": "Online Computer Store",
  "ToAddress": ""
}
```

With `Enabled: true` and real SMTP credentials, the site sends real email for the contact
form, order confirmations, password-reset links, low-stock alerts, and checkout verification
codes. For Gmail, `Username`/`FromAddress` is your Gmail address and `Password` is a 16-character
**App Password** (Google Account → Security → 2-Step Verification → App passwords) — not your
normal Gmail password.

**Recommended:** keep `Password` out of `appsettings.json` and use user-secrets instead:

```
dotnet user-secrets set "Smtp:Enabled" "true"
dotnet user-secrets set "Smtp:Password" "your-16-character-app-password"
```

With `Enabled: false` (or no password set), the contact form and account emails simply report
that email isn't configured yet — this is expected, not a bug.

### Payments (Stripe, test mode)

```json
"Stripe": {
  "Enabled": true,
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_...",
  "WebhookSecret": "",
  "Currency": "usd"
}
```

Checkout uses Stripe's hosted Checkout page. With `Enabled: true` and real **test**
keys from https://dashboard.stripe.com/test/apikeys (the URL must contain `/test/` —
that's how you know no real money can move), placing an order redirects to Stripe,
takes a test card, and redirects back to a server-verified confirmation page. Use
Stripe's test card `4242 4242 4242 4242`, any future expiry, any CVC.

With `Enabled: false` (or no keys set), checkout falls back to placing the order
directly with no payment step — handy for quick demos.

**Recommended:** rather than pasting `SecretKey` into `appsettings.json`, use user-secrets:

```
dotnet user-secrets set "Stripe:Enabled" "true"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_...your key..."
```

(`PublishableKey` is designed to be public — it's fine left in `appsettings.json` and is
even sent to the browser during checkout — only `SecretKey` needs to stay private.)

**Never commit real keys.** `appsettings.json` is already listed in `.gitignore`;
copy `appsettings.example.json` to `appsettings.json` (git-ignored) if you're setting
this up fresh, and keep your real keys only in the local, untracked copy.

### Local secrets (recommended) vs. production secrets

`.gitignore` already keeps your real `appsettings.json` out of source control, which covers
the main risk (secrets ending up in a public repo). For extra safety — so a real password or
API key never sits in a plaintext file at all, even one that's git-ignored — use **user-secrets**
for local development, as shown above for each setting. They're already wired up (the project
has a `UserSecretsId`), so `dotnet user-secrets set "Section:Key" "value"` from the project
folder (or Visual Studio → right-click project → **Manage User Secrets**) is all that's needed;
values set this way override `appsettings.json` automatically whenever you run in Development
(F5 already does this — see `Properties/launchSettings.json`).

If you ever deploy this somewhere (a real server, a container, Azure, etc.) instead of just
running it locally, use **environment variables** instead — user-secrets only apply in
Development. ASP.NET Core reads environment variables automatically, using a double-underscore
in place of the `:` in the setting's name, and they take priority over `appsettings.json`:

```
Smtp__Password=your-app-password
Ai__ApiKey=sk-...your key...
Stripe__SecretKey=sk_live_...your key...
```

No code changes are needed for either of these — both are already picked up automatically by
the configuration system the app uses.

## Product images

All 15 catalog items now use real product photography. The `wwwroot/images/` folder contains:

- **Photos you supplied directly** — used as-is (converted to JPEG) for: Dell XPS 13,
  HP Pavilion Desktop, HP Spectre x360, Logitech MX Master mouse, and the MacBook Air.
- **Two real banner photos you supplied** — the 3-laptops lifestyle photo (`hero-laptops.jpg`,
  used as the homepage hero background) and the rendered store interior
  (`store-showroom.jpg`, used as the homepage "Also open in-store" section background).
- **Ten product photos cropped from retailer listing screenshots you supplied** — for the
  other 10 catalog items (Lenovo ThinkPad X1 Carbon, ASUS ROG Strix G16, Samsung Galaxy
  Book4, iPad Pro, Corsair keyboard, Sony WH-1000XM5, Dell UltraSharp monitor, Razer
  Basilisk mouse, Samsung T7 SSD, Logitech Brio webcam). Each was cropped down to just the
  product shot — removing the retailer page's price, breadcrumbs, badges, and buttons —
  then flattened onto a plain white background to match the style of the other photos.

Swap in a different photo any time by replacing the matching `.jpg` file in
`wwwroot/images/` (keep the same filename — it's referenced from `Data/DbInitializer.cs`).

## Folder structure

| Folder | Purpose |
|---|---|
| `Controllers/` | MVC controllers — Home, Shop, Cart, Checkout, Account, Orders, Wishlist, Search, Compare, Assistant, Admin |
| `Models/` | Data models and view models |
| `Components/` | ViewComponents — `CartBadge`, `WishlistBadge`, `UserAvatar` (re-rendered on every page load) |
| `Data/` | `StoreDbContext` (EF Core) and `DbInitializer` (seed data) |
| `Services/` | Business logic (products, cart, orders, users, wishlist, reviews, audit log, email) and the AI service layer |
| `Views/` | Razor views |
| `wwwroot/` | CSS, JS, images, and third-party front-end libraries (Bootstrap, jQuery) |
| `OnlineComputerStore.Tests/` | xUnit test project (sibling to `OnlineComputerStore.Core/`, same solution) |

## Running the tests

From a terminal at the solution root (or Visual Studio's Test Explorer):

```
dotnet test
```

Coverage is currently partial — `OrderService` is tested (including the low-stock alert
behavior), but `CartService`, `UserService`, and `StripePaymentService` don't have tests yet.

## Known simplifications

- Accounts use a lightweight custom cookie-based login (PBKDF2 password hashing) rather than
  the full ASP.NET Core Identity framework, to keep the codebase easy to read.
- The `/Admin` area is restricted to admins only (`[Authorize(Roles = "Admin")]`). The very
  first account ever registered is automatically promoted to admin on startup; to hand admin
  rights to a different account instead, flip the `IsAdmin` column for the right rows in the
  `Users` table (or clear it from everyone and restart to re-bootstrap).
- Order tracking (`/Orders/Track`) requires both the order ID and the email address used at
  checkout, so guessing another customer's short order ID isn't enough to see their order.
- Payment uses Stripe Checkout in **test mode** — see "Payments (Stripe, test mode)" above.
  No real money moves as long as you use test (`pk_test_`/`sk_test_`) keys.
- Profile photos are capped at 1 MB and stored directly on the account row (not as files on
  disk) — plenty for an avatar, not meant for anything larger.
- A separate, legacy ASP.NET Web Forms prototype (a different codebase, not this one, in the
  `1. WEBSITE Online Computer Store` folder) predates this project and isn't part of it — see
  the `README.md` in that folder and `DOCUMENTATION.md` §10 for what it is and why it's kept
  out of this repo.

## Rebuilding the database from scratch

Delete `onlinecomputerstore.db` (created next to the app after the first run) and restart —
it will be recreated and reseeded automatically. Any accounts/orders you'd created will be
lost, so only do this if you want a clean slate.

## Publishing this to GitHub

This folder is not yet a Git repository. `.gitignore` is already set up to keep your real
secrets out — it excludes `appsettings.json` (your real config lives only in `dotnet
user-secrets`, per "Configuration" above), the SQLite database files, and build output
(`bin/`, `obj/`, `.vs/`). `appsettings.example.json`, which only has placeholder values, is
what gets committed instead.

From a terminal at this folder (the one with `OnlineComputerStore.sln` in it):

```
git init
git add .
git commit -m "Initial commit"
```

Then create an empty repository on https://github.com/new (don't check "Add a README" —
this folder already has one) and push:

```
git remote add origin https://github.com/<your-username>/<repo-name>.git
git branch -M main
git push -u origin main
```

Before your first push, it's worth double-checking `git status` doesn't list
`appsettings.json` as a new/changed file — if it does, `.gitignore` isn't catching it (check
you're running these commands from this exact folder) and it should be excluded before you
commit. Everything else generated locally (the `.db` file, `bin/`, `obj/`, `.vs/`) is excluded
the same way and will simply be recreated for anyone who clones the repo and runs it.

**Don't add the separate `1. WEBSITE Online Computer Store` folder to this repository** — see
"Known simplifications" above and that folder's own `README.md` for why.
