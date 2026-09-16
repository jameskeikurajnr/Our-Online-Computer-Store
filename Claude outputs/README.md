# Online Computer Store — ITEC322 Capstone

**Project:** A full ASP.NET Core 8 MVC e-commerce rebuild of a legacy Online Computer Store,
with an optional AI layer (search, on-site assistant, AI-written copy) and Stripe payments in
test mode.

**Status as of Sept 16, 2026:** Feature-complete, hardened, verified running locally, and just
through a storefront visual-design pass (Phase 14).

## What's tracked here

| Doc | Purpose |
|---|---|
| `SPRINT.md` | What shipped this sprint and what was fixed |
| `BACKLOG.md` | What's left before final submission, prioritized |
| This doc | Project overview and where to find things |

The full Work Breakdown Structure (14 phases, task-by-task, with estimates and dates) lives in
`clickup_import_wbs.csv` — see "Importing the WBS into ClickUp" below.

## Project state, in brief

The core storefront (catalog, cart, checkout with Stripe, order tracking, accounts, admin
dashboard, AI-assist features) has been feature-complete since Phase 5–6. Phase 10 added a
round of feature enhancements (wishlist, reviews, forgot-password, audit log, Click & Collect,
and more). Phase 11 added CI/CD (GitHub Actions + CodeQL + Dependabot), a redesigned account
menu, a theme toggle, and expanded the catalog to 27 items. Phase 12 was a full audit that
found and fixed several severe defects that had crept in during Phase 11's file-by-file
editing — most seriously, the entire site had lost its shared page layout. Phase 13 added a
new store-operations feature: unmatched customer searches are now logged, and once one clearly
looks like a pattern (more than one search) the admin gets emailed so the item can be sourced
and listed, with a new Admin > Not on Website page to manage the list. **Phase 14** (this
sprint) took a visual-design pass across the pages that hadn't been touched since the original
build — Contact, About, the Home hero, the AI assistant panel, and Track Order — fixed the two
hero buttons that did the same thing (one now genuinely filters to Laptops), grew the Laptops
category from 6 to 12 items (33 catalog items overall), and along the way found and fixed two
real bugs the expansion had introduced (a pagination size that hid half the new laptops, and a
CSS specificity conflict on the redesigned Track Order validation banner).

What's left is almost entirely assessment deliverables (final report, two presentations, final
submission), filling in the remaining unit test gaps (`CartService`, `UserService`,
`StripePaymentService`, `ProductRequestService`, and now the new Shop category-filter logic),
and sourcing real product photography for the 6 laptops added this sprint — see `BACKLOG.md`
for the full list.

## Repository

The codebase itself carries its own `README.md` (setup in Visual Studio, `appsettings.json`
configuration, running tests, folder structure) and `DOCUMENTATION.md` (architecture, data
model, security model, and — new this sprint — §11 on the Phase 14 storefront work). This doc
is the ClickUp-side project overview, not a replacement for either.

## Importing the WBS into ClickUp

`clickup_import_wbs.csv` is formatted for ClickUp's native CSV importer, with one row per task
and `Parent Task` linking subtasks to their phase:

1. In ClickUp, go to the **ITEC322** Space (or wherever you want the WBS to live) and open
   **Import** from the sidebar (or **Everything → Import**, depending on your ClickUp layout).
2. Choose **CSV** as the import source and upload `clickup_import_wbs.csv`.
3. On the column-mapping screen, map each column to its matching ClickUp field:
   - `Task Name` → Task Name
   - `Parent Task` → Parent (ClickUp will nest subtasks under the row whose Task Name matches)
   - `Description` → Description
   - `Status` → Status (map `Complete` / `In Progress` / `To Do` to your Space's statuses —
     ClickUp will prompt you to match these if they don't already exist)
   - `Priority` → Priority
   - `Tags` → Tags (comma-separated values in one cell become multiple tags)
   - `Start Date` / `Due Date` → Start Date / Due Date
   - `Time Estimate` → Time Estimate
   - `List Name` → List (every row uses the same list, "Online Computer Store - Capstone WBS",
     so this creates or targets one List)
4. Preview the import — ClickUp shows a row count and a sample of how tasks will nest. Confirm
   the phase rows (no `Parent Task` value) come in as parent tasks and the numbered rows
   (1.1, 1.2, …) nest under them.
5. Run the import. If this is a re-import over an existing WBS list, note that ClickUp's CSV
   importer creates new tasks rather than updating existing ones by name — so either delete the
   old list first or import into a fresh list, rather than ending up with duplicates. If you've
   already imported an earlier version of this file (e.g. through Phase 13), importing this one
   fresh will duplicate everything up to 13.6 — either delete that list first, or manually add
   just the new Phase 14 rows (14.0–14.9) to your existing ClickUp list instead of re-importing
   the whole CSV.
6. Once imported, spot-check a couple of phases (Phase 14 is the newest) to confirm dates,
   tags, and nesting landed as expected.

## Where this file lives

These project-tracking docs (this file, `DOCUMENTATION.md`, `BACKLOG.md`, `SPRINT.md`,
`clickup_import_wbs.csv`) are kept in `Claude outputs/` alongside the repository rather than
inside the ASP.NET Core project itself, so they're easy to find without digging through
`OnlineComputerStore.Core/`.

## Committing this sprint's work

**Update (Sept 16):** when this was actually run, `git status` showed a lot more than just
Phase 14 — several earlier sprints' work (Phase 10–13: wishlist, account/admin redesign, the
"Not on Website" audit-log feature, AI assistant refinements) had also never been committed.
It also turned up a `Middleware/`, `Models/PageView.cs`, and an `AddPageViews` migration that
nobody could immediately account for — those were deliberately left out of the commit below
until their origin is confirmed (check their file timestamps in Explorer — Properties →
Details — to see whether they're old scaffolding or something written more recently).

The `.db`/`.db-shm`/`.db-wal` files are already git-ignored (`*.db*` in `.gitignore`), so the
live-database update mentioned in `SPRINT.md` doesn't need a commit. From a terminal in the
repo root — **paste this as one single line** (a multi-line backtick-continued version of this
command was tried first and silently failed to stage anything when pasted into the Developer
PowerShell pane, most likely because the paste didn't preserve the line continuations):

```powershell
git status
```

```powershell
git add "OnlineComputerStore.Core/Controllers/AdminController.cs" "OnlineComputerStore.Core/Controllers/AssistantController.cs" "OnlineComputerStore.Core/Controllers/ShopController.cs" "OnlineComputerStore.Core/Data/DbInitializer.cs" "OnlineComputerStore.Core/Data/StoreDbContext.cs" "OnlineComputerStore.Core/Migrations/StoreDbContextModelSnapshot.cs" "OnlineComputerStore.Core/Program.cs" "OnlineComputerStore.Core/Services/AiAssistantService.cs" "OnlineComputerStore.Core/Services/AiServiceBase.cs" "OnlineComputerStore.Core/Services/IAiAssistantService.cs" "OnlineComputerStore.Core/Views/Account/Login.cshtml" "OnlineComputerStore.Core/Views/Account/Register.cshtml" "OnlineComputerStore.Core/Views/Admin/AuditLog.cshtml" "OnlineComputerStore.Core/Views/Admin/Dashboard.cshtml" "OnlineComputerStore.Core/Views/Admin/Orders.cshtml" "OnlineComputerStore.Core/Views/Admin/ProductRequests.cshtml" "OnlineComputerStore.Core/Views/Admin/Products.cshtml" "OnlineComputerStore.Core/Views/Cart/Index.cshtml" "OnlineComputerStore.Core/Views/Home/About.cshtml" "OnlineComputerStore.Core/Views/Home/Contact.cshtml" "OnlineComputerStore.Core/Views/Home/Index.cshtml" "OnlineComputerStore.Core/Views/Orders/Track.cshtml" "OnlineComputerStore.Core/Views/Orders/TrackResult.cshtml" "OnlineComputerStore.Core/Views/Shared/_AssistantWidget.cshtml" "OnlineComputerStore.Core/Views/Shared/_Layout.cshtml" "OnlineComputerStore.Core/Views/Shop/Details.cshtml" "OnlineComputerStore.Core/Views/Shop/Index.cshtml" "OnlineComputerStore.Core/Views/Wishlist/Index.cshtml" "OnlineComputerStore.Core/wwwroot/css/assistant.css" "OnlineComputerStore.Core/wwwroot/css/site.css" "OnlineComputerStore.Core/wwwroot/js/assistant.js" "OnlineComputerStore.Core/wwwroot/images/placeholder-acer-swift-go.jpg" "OnlineComputerStore.Core/wwwroot/images/placeholder-dell-inspiron.jpg" "OnlineComputerStore.Core/wwwroot/images/placeholder-lg-gram.jpg" "OnlineComputerStore.Core/wwwroot/images/placeholder-microsoft-surface.jpg" "OnlineComputerStore.Core/wwwroot/images/placeholder-msi-stealth.jpg" "OnlineComputerStore.Core/wwwroot/images/placeholder-razer-blade.jpg" "OnlineComputerStore.Core/wwwroot/images/product-placeholder.jpg" "DOCUMENTATION.md" "README.md" "README-1.md" "Claude outputs/BACKLOG.md" "Claude outputs/DOCUMENTATION.md" "Claude outputs/README.md" "Claude outputs/SPRINT.md" "Claude outputs/clickup_import_wbs.csv"
```

```powershell
git status
```

```powershell
git commit -m "Catch up uncommitted work through Phase 14: account/admin views, AI assistant refinements, wishlist, and storefront UX modernization"
git push origin main
```

Run `git status` before *and* after the `git add` — before, to confirm the list above still
matches what's actually changed (this project's history has shown that "what I touched this
session" and "what's uncommitted" can drift apart); after, to confirm only the intended files
are staged and that `Middleware/`, `Models/PageView.cs`, and the `AddPageViews` migration files
are still sitting untouched under "Untracked files" until you've confirmed what they are. If
`git status` after the `git add` still shows nothing staged, the paste broke again — try typing
`git add ` followed by pasting just the quoted file list, or paste into a plain `cmd.exe`/
Windows Terminal window instead of the Developer PowerShell pane.
`README.md`/`README-1.md`/`DOCUMENTATION.md` at the repo root show as deleted — that's expected
(superseded by the `Claude outputs/` versions) and safe to include in the commit.
