# Current Sprint — Production Hardening & Bug Remediation

**Dates:** Sept 14–15, 2026
**Status:** Complete

## Goal

Take the Online Computer Store from "feature-complete" to "actually runs correctly end to
end" — add the CI/CD and security scaffolding a capstone submission should have, then find
and fix whatever broke along the way.

## What shipped this sprint

**DevOps & CI/CD**
- GitHub Actions workflow (`build-and-test.yml`) — restores, builds, and runs the test suite
  on every push/PR to `main`
- CodeQL security scanning (`codeql.yml`) — on every push/PR plus a weekly scheduled run
- Dependabot (`dependabot.yml`) — weekly PRs for outdated NuGet packages and GitHub Actions

**UX & catalog**
- Redesigned account menu (avatar + name dropdown replacing a bare text link)
- Light/dark theme toggle, remembered across visits
- Bootstrap Icons self-hosted (no CDN dependency)
- Catalog expanded from 15 to 27 items — added MFA & Security Keys, Cybersecurity, and USB
  Drives categories, plus a Chargers category

**Security**
- Rate limiting extended to the forgot-password endpoint (3 attempts / 10 min per IP),
  matching the existing login limiter (5 / 5 min)

## Critical bugs found and fixed (same sprint, static audit)

A full audit turned up several severe defects that had crept in alongside the work above.
Worst first:

1. **`Views/_ViewStart.cshtml` was missing.** This is the file that tells every Razor view to
   use `_Layout.cshtml`. With it gone and no view setting `Layout` itself, *every page on the
   site* would have rendered as a bare, unstyled fragment — no header, no nav, no footer, no
   theme script. This was the single biggest defect in the whole codebase. **Fixed.**
2. **All of `wwwroot/images/` was missing** — all 27 product photos and both homepage hero
   banners. **Fixed** — recovered from a prior working copy and re-placed.
3. **jQuery, jQuery Validation, Bootstrap's JS bundle, and `assistant.js` were all missing**
   from `wwwroot/lib` / `wwwroot/js`, along with `_ValidationScriptsPartial.cshtml` (called by
   11 different forms — login, register, checkout, etc. — which would have thrown a runtime
   error with it gone). **Fixed.**
4. **Site-wide CSRF gap.** Only `CheckoutController` was validating anti-forgery tokens —
   login, register, cart, wishlist, contact, and every admin action accepted POSTs without
   checking the token the forms were already sending. **Fixed** with one global filter in
   `Program.cs` (with the one legitimate JSON/fetch endpoint explicitly exempted).
5. The local `.git` folder was a broken half-initialized stub. **Fixed** — real repo
   initialized, first commit made, secrets confirmed excluded.
6. README was stale (still said "15 catalog items," still said "not yet a Git repository").
   **Fixed.**

**Caveat:** the audit environment couldn't reach NuGet, so these fixes are verified by static
review (every asset reference resolves, every referenced partial view exists) rather than by
an actual `dotnet build`/`dotnet test`. You've since rebuilt and run it locally and confirmed
it's working — that's the real verification.

## Addendum (Sept 15) — Phase 13: "Not on Website" missing-product alerts

Shipped after the sprint above closed, same day.

- New `ProductRequests` table (hand-written EF Core migration) logs any customer search that
  matches nothing in the catalog, deduplicating case-insensitively so repeat searches for the
  same term bump a counter instead of creating new rows.
- Once a term crosses **2 searches**, the admin gets a one-time email (subject includes the
  search count) — not on every miss, so a single popular missing item doesn't spam the inbox.
- New **Admin > Not on Website** page: lists unresolved misses newest-first, a search-count
  badge that highlights once a term has crossed the alert threshold, a per-row "Handled"
  action, and "Clear All".
- Search results page's empty state now tells the customer their search was noted, instead of
  a bare "no products matched."
- **Bug found and fixed same day:** adding the new method to `IEmailService` broke the build —
  `OnlineComputerStore.Tests`' `FakeEmailService` test double hadn't been updated to implement
  it. Fixed by adding the matching no-op method there too.

## Next sprint candidates

See `BACKLOG.md`.
