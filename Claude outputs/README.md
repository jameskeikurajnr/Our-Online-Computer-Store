# Online Computer Store — ITEC322 Capstone

**Project:** A full ASP.NET Core 8 MVC e-commerce rebuild of a legacy Online Computer Store,
with an optional AI layer (search, on-site assistant, AI-written copy) and Stripe payments in
test mode.

**Status as of Sept 16, 2026:** Feature-complete, hardened, verified running locally, through a
storefront visual-design pass (Phase 14) plus a new site-traffic tracking feature, and fully
committed and pushed to GitHub (`origin/main` @ `97e96af`).

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
CSS specificity conflict on the redesigned Track Order validation banner). Alongside Phase 14,
a page-view tracking feature (`PageViewMiddleware`) landed too, replacing the Admin Dashboard's
hardcoded "Traffic" sample data with a real count of page visits.

What's left is almost entirely assessment deliverables (final report, two presentations, final
submission), filling in the remaining unit test gaps (`CartService`, `UserService`,
`StripePaymentService`, `ProductRequestService`, the new Shop category-filter logic, and now
`PageViewMiddleware`'s counting logic), and sourcing real product photography for the 6
laptops added this sprint — see `BACKLOG.md` for the full list.

## Repository

The repo root carries its own `README.md` — setup in Visual Studio, `appsettings.json`
configuration, running tests, folder structure. It was briefly deleted during the Sept 16
git catch-up (mistaken for a duplicate of this file) and restored once the mistake was caught
— see this doc's "Post-sprint" section below. `DOCUMENTATION.md` (architecture, data model,
security model, and — new this sprint — §11 on the Phase 14 storefront work, §12 on PageView
tracking) lives here in `Claude outputs/` alongside this file. This doc is the ClickUp-side
project overview, not a replacement for either.

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
   just the new Phase 14 rows (14.0–14.11) to your existing ClickUp list instead of re-importing
   the whole CSV.
6. Once imported, spot-check a couple of phases (Phase 14 is the newest) to confirm dates,
   tags, and nesting landed as expected.

## Where this file lives

These project-tracking docs (this file, `DOCUMENTATION.md`, `BACKLOG.md`, `SPRINT.md`,
`clickup_import_wbs.csv`) are kept in `Claude outputs/` alongside the repository rather than
inside the ASP.NET Core project itself, so they're easy to find without digging through
`OnlineComputerStore.Core/`.

## Committing this sprint's work

**Everything is committed and pushed as of Sept 16, 2026.** For the record, here's what
actually happened getting there:

`git status` first turned up several earlier sprints' work (Phase 10–13: wishlist,
account/admin redesign, the "Not on Website" audit-log feature, AI assistant refinements) that
had never been committed, on top of this sprint's own changes. All of it — plus the root-level
`README.md`/`README-1.md`/`DOCUMENTATION.md`, superseded by their `Claude outputs/` equivalents
— went into one catch-up commit: `2a1b6ed`.

That same `git status` also turned up an untracked page-view tracking feature (`PageView`
model, `AddPageViews` migration, `PageViewMiddleware`) of unclear origin, deliberately held
back from that first commit pending confirmation. That turned out to matter: `Program.cs` and
`StoreDbContext.cs` — already committed in the same push — directly referenced it, so leaving
it out meant the pushed repo couldn't actually compile from a fresh clone. Once traced, it was
committed on its own: `13b15bb`, followed by a small doc-only commit recording all of this in
`SPRINT.md`/`BACKLOG.md`: `97e96af`.

**Current state:** `git status` in the repo root should show nothing outstanding except
`commit-phase14.ps1` (the staging script used for the catch-up commit — harmless to keep
around or delete). The `.db`/`.db-shm`/`.db-wal` files are git-ignored (`*.db*` in
`.gitignore`) and were never part of any commit — that's expected, not an oversight.
