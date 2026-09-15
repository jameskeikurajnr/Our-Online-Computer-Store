# Backlog

Everything left before final submission, roughly in priority order. Pulled from the WBS
(`clickup_import_wbs.csv`) and the "Known simplifications" section of the project README.

## High priority — assessment-critical

- **Final Project Report (40%)** — assemble from README.md, DOCUMENTATION.md, and the testing
  evidence already written up. (WBS 8.5)
- **Project Progress Presentation (30%)** — current build status, security fixes, payment
  integration. (WBS 9.1)
- **Final Project Presentation (30%)** — the completed, tested storefront including Stripe
  payment flow and security fixes. (WBS 9.2)
- **Final submission & deployment demo** — package the repo + docs + WBS, prepare a live or
  recorded demo. (WBS 9.3)

## Medium priority — test coverage

- Unit tests for `CartService` (add/remove/update-quantity/merge-on-login logic untested)
- Unit tests for `UserService` (registration, password hashing/verification, admin
  auto-bootstrap logic untested)
- Unit tests for `StripePaymentService` (Checkout Session creation and server-side payment
  verification untested — likely needs a fake `HttpMessageHandler` rather than hitting Stripe)
- A manual click-through on an actual phone and desktop browser before submission — the
  responsive/cross-browser pass so far has been static/code-level review only, not a live
  browser session (WBS 6.5 note)

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

## Explicitly out of scope (documented decisions, not backlog items)

- Full ASP.NET Core Identity — the lightweight custom cookie-based auth was a deliberate
  choice to keep the codebase readable (README "Known simplifications")
- The legacy ASP.NET Web Forms prototype — retire, don't repair (WBS Phase 7)
