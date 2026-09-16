# Current Sprint — Storefront UX Modernization & Catalog Expansion

**Dates:** Sept 15–16, 2026
**Status:** Complete
**WBS:** Phase 14 (see `clickup_import_wbs.csv`)
**Previous sprint:** `SPRINT-1.md` (Phase 11–13: DevOps/CI, critical bug remediation, "Not on Website" alerts)

## Goal

Take the customer-facing storefront from "functionally complete" to "looks and reads like a
real product" — a visual-design pass across the pages that hadn't been touched since the
original build, plus fixing a genuine UX gap (two hero buttons doing the same thing) and
growing the Laptops category.

Every page below went through the same process before anything real was touched: a static
HTML mockup reusing the site's actual design tokens → light/dark/mobile screenshots →
approval → the real `.cshtml`/`.cs` change, preserving every existing model binding and
`asp-*` attribute exactly → a Playwright-based verification pass (rendering the *actual*
shipped file's markup, not the mockup) checking for console errors and correct light/dark/
mobile rendering → push.

## What shipped this sprint

**Storefront redesign**
- Store Assistant's product-card panel rebuilt as a horizontal scroll-snap carousel
- Contact Us page redesigned (icon-prefixed inputs, styled validation states, working
  `mailto:` link)
- About Us page redesigned (SVG icons replacing emoji, new value cards)
- Track Order lookup + result pages redesigned, including a **new Order Summary section**
  (line items + total) on the result page, built from `Order.Items`/`Order.Total` data that
  existed but was never shown anywhere
- Home hero headline restyled — bigger/bolder, a teal gradient highlight on "Perfect
  Computer," centered within its column

**Catalog & UX fix**
- Diagnosed and fixed the "two hero buttons do the same thing" request: `ShopController`
  gained real category filtering (case-insensitive, preserved through sort/pagination), and
  Home's "Shop Laptops" button now actually filters to Laptops
- Laptops category expanded from 6 to 12 items (Microsoft Surface Laptop 6, Acer Swift Go 14,
  Razer Blade 16, LG Gram 17, Dell Inspiron 15, MSI Stealth 15M) — verified against the live
  SQLite database (not just the seed file) before adding, since the customer's assumed
  starting count (10) didn't match reality (6)
- Catalog now sits at **33 items** total

## Bugs found and fixed this sprint

1. **Shop page size hid half the new laptops.** `ShopController.PageSize` was `8`; with
   Laptops now at 12 items, the default "Shop Laptops" view only showed the first 8
   alphabetically — 3 of the 6 newly-added laptops were sitting on an unseen page 2. The
   customer reported this as "the laptops aren't loaded" after restarting their computer and
   rebuilding, which ruled out a caching/build issue and pointed at pagination instead.
   Confirmed via the live database (all 12 rows present) before changing anything. **Fixed**
   by raising `PageSize` to 12, so the largest category fits on one page.
2. **Validation summary color conflict on Track.cshtml.** The redesigned validation-error
   banner uses a custom amber palette, but the `asp-validation-summary` div had kept its
   original `class="text-danger"` — a Bootstrap utility that sets color with `!important`,
   which silently overrode the new styling and rendered the icon/text red instead of amber.
   Found during the real-file Playwright verification pass (not visible in the mockup, which
   didn't carry the leftover class). **Fixed** by dropping the now-redundant `text-danger`
   class.
3. **"Not loaded" images were actually a labeling gap, not a bug.** After the catalog fix
   above, the customer separately reported the 6 new laptops' images "still not loaded." All
   6 image files were confirmed present on disk and picked up by the most recent build — what
   the customer was seeing was the intentional "Photo coming soon" placeholder graphic (no
   licensed product photography exists for these 6 yet), just not recognized as a working
   placeholder rather than a failure. Resolved by upgrading the placeholder from one shared
   generic gray box to 6 distinct, brand-colored versions (see below) so each reads more
   obviously as "this specific product, photo pending" rather than "something broke."

## A note on product photography

The customer initially asked for real photos for the 6 new laptops and supplied screenshots
from Amazon/eBay/LG.com/PCMag/other retail listings. These were **not used** — they're
professional product photography owned by the respective manufacturers/retailers, not
licensed for reuse here, regardless of who does the copying. Declined consistently across two
rounds (including a re-crop of the same source images), with the reasoning explained each
time. Shipped instead: 6 locally-generated placeholder graphics, one per laptop, each with its
own accent color and a plain-text brand label (no logos) so the grid reads as distinct
listings rather than six copies of the same box. Real photography is now a backlog item — see
`BACKLOG.md`.

## Post-sprint: committing accumulated work & a near-miss broken build

**Date:** Sept 16, 2026

When it came time to actually commit this sprint's work, `git status` showed far more than
Phase 14 — several earlier sprints (Phase 10–13: wishlist, account/admin redesign, the "Not on
Website" audit-log feature, AI assistant refinements) had also never been committed. All of it
was reconciled and pushed in one commit (`2a1b6ed`), alongside the root-level `README.md`/
`README-1.md`/`DOCUMENTATION.md`, which had been superseded by their `Claude outputs/`
equivalents and were removed as part of the same commit.

That same `git status` also turned up an untracked page-view tracking feature — a `PageView`
model, an `AddPageViews` EF Core migration, and a `PageViewMiddleware` that logs one row (path
+ timestamp, nothing identifying) per real page request, feeding the admin dashboard's
"Traffic" line with real numbers instead of the hardcoded sample data it used to show. Its
origin wasn't immediately clear, so it was deliberately left out of the first catch-up commit
pending confirmation.

That turned out to matter: `Program.cs` and `StoreDbContext.cs` — both already committed in
that same push — directly reference `PageViewMiddleware` and `PageView`. Leaving those two
files out meant the pushed repo would fail to compile on a fresh clone: exactly the kind of
thing that would surface badly during a graded submission or live demo. Once the dependency was
traced, the feature was committed on its own as legitimate, finished work rather than stray
scaffolding.

**Takeaway:** commit more often. A gap this size made it hard to tell what belonged to which
sprint, and let a build-breaking half-commit slip through what looked like a routine catch-up.

## Next sprint candidates

See `BACKLOG.md`.
