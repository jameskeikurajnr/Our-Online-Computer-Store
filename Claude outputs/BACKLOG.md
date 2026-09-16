# Backlog

Everything left before final submission, roughly in priority order. Pulled from the WBS
(`clickup_import_wbs.csv`) and the "Known simplifications" section of the project
`DOCUMENTATION.md`.

## High priority — assessment-critical

- **Final Project Report (40%)** — assemble from `README.md`, `DOCUMENTATION.md`, and the
  testing evidence already written up. (WBS 8.5)
- **Project Progress Presentation (30%)** — current build status, security fixes, payment
  integration. (WBS 9.1)
- **Final Project Presentation (30%)** — the completed, tested storefront including Stripe
  payment flow, security fixes, and this sprint's UX modernization pass. (WBS 9.2)
- **Final submission & deployment demo** — package the repo + docs + WBS, prepare a live or
  recorded demo. (WBS 9.3)

## Medium priority — test coverage

- Unit tests for `CartService` (add/remove/update-quantity/merge-on-login logic untested)
- Unit tests for `UserService` (registration, password hashing/verification, admin
  auto-bootstrap logic untested)
- Unit tests for `StripePaymentService` (Checkout Session creation and server-side payment
  verification untested — likely needs a fake `HttpMessageHandler` rather than hitting Stripe)
- Unit tests for `ProductRequestService` (dedup logic and the 2-search alert threshold are
  untested — the only coverage so far is the manual fix to `FakeEmailService`) (WBS 13.2/13.3)
- **New:** `ShopController`'s category-filtering logic (WBS 14.4) and the page-size fix
  (WBS 14.8) have no automated coverage yet — worth a couple of focused tests given a filtered,
  paginated view already had one real bug slip through to the customer this sprint
- A manual click-through on an actual phone and desktop browser before submission — the
  responsive/cross-browser pass so far has been static/code-level review only (plus the
  Playwright-rendered mockup screenshots from this sprint, which cover the *redesigned* pages
  but not the rest of the site) (WBS 6.5 note)
- **New:** `PageViewMiddleware`'s `ShouldCount` logic (skips admin-dashboard views, API calls,
  and static-file-looking paths; only counts GET + 200 responses) and its swallowed-exception
  behavior on save failure are untested — worth a couple of unit tests given it writes to the
  database on every real page load.
- **New:** Confirm the `PageViews` table has actually been created on the live database (via
  `dotnet ef database update` or however this project applies migrations) and that Admin >
  Dashboard is rendering real traffic numbers now that the middleware is committed, not just
  compiling successfully.

## Medium priority — catalog

- **New:** Real product photography for the 6 laptops added this sprint (Microsoft Surface
  Laptop 6, Acer Swift Go 14, Razer Blade 16, LG Gram 17, Dell Inspiron 15, MSI Stealth 15M) —
  currently showing a locally-generated, brand-colored "Photo coming soon" placeholder. Needs
  either the customer's own photography or a licensed source; see `SPRINT.md` for why the
  retail-listing screenshots offered this sprint weren't used.

## Low priority — nice-to-haves / follow-ups

- Confirm the `dotnet build`/`dotnet test` actually run clean in a normal dev environment with
  NuGet access (you've done this already — worth a note in the WBS/testing section once you
  see the test-run output, since the last audit could only verify statically)
- Consider whether the legacy Web Forms prototype folder should be archived somewhere outside
  this repo entirely, per the Phase 7 decision (retire, don't repair)
- Revisit whether `CartController` and `HomeController.ClearBrowseHistory` need any
  finer-grained rate limiting now that the global anti-forgery filter is in place (the two are
  independent protections — anti-forgery stops cross-site forgery, rate limiting stops brute
  force/abuse from a legitimate origin)
- **New:** This sprint's design pass covered the AI assistant panel, Contact, About, Home
  hero, and Track Order. Cart, Checkout, Wishlist, and the Orders/Admin views haven't been
  through the same mockup-and-approval visual pass — worth a look if there's time, though
  none of them are known to be broken, just visually unreviewed against the newer pages'
  styling.
- **New:** Commit more frequently going forward. Several sprints' worth of work (Phase 10–14)
  had accumulated uncommitted before a single catch-up commit on Sept 16, and a half-committed
  feature (PageView tracking) briefly left the pushed repo in a non-compiling state until it
  was caught — see `SPRINT.md`. Smaller, more frequent commits make this kind of gap much
  easier to spot early.

## Explicitly out of scope (documented decisions, not backlog items)

- Full ASP.NET Core Identity — the lightweight custom cookie-based auth was a deliberate
  choice to keep the codebase readable (`DOCUMENTATION.md` §8, "Known simplifications")
- The legacy ASP.NET Web Forms prototype — retire, don't repair (WBS Phase 7)
- Reproducing manufacturer/retailer product photography without a license — declined this
  sprint for the 6 new laptops; see `SPRINT.md`
