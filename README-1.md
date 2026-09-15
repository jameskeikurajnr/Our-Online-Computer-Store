# Online Computer Store — ITEC322 Capstone

**Project:** A full ASP.NET Core 8 MVC e-commerce rebuild of a legacy Online Computer Store,
with an optional AI layer (search, on-site assistant, AI-written copy) and Stripe payments in
test mode.

**Status as of Sept 15, 2026:** Feature-complete, hardened, and verified running locally.

## What's tracked here

| Doc | Purpose |
|---|---|
| `SPRINT.md` | What shipped this sprint and what was fixed |
| `BACKLOG.md` | What's left before final submission, prioritized |
| This doc | Project overview and where to find things |

The full Work Breakdown Structure (12 phases, task-by-task, with estimates and dates) lives in
`clickup_import_wbs.csv` — see "Importing the WBS into ClickUp" below.

## Project state, in brief

The core storefront (catalog, cart, checkout with Stripe, order tracking, accounts, admin
dashboard, AI-assist features) has been feature-complete since Phase 5–6. Phase 10 added a
round of feature enhancements (wishlist, reviews, forgot-password, audit log, Click & Collect,
and more). Phase 11 added CI/CD (GitHub Actions + CodeQL + Dependabot), a redesigned account
menu, a theme toggle, and expanded the catalog to 27 items. Phase 12 was a full audit that
found and fixed several severe defects that had crept in during Phase 11's file-by-file
editing — most seriously, the entire site had lost its shared page layout. Everything in
Phase 12 has since been rebuilt and run successfully.

What's left is almost entirely assessment deliverables (final report, two presentations, final
submission) plus filling in the remaining unit test gaps (`CartService`, `UserService`,
`StripePaymentService`) — see `BACKLOG.md` for the full list.

## Repository

The codebase itself carries its own `README.md` (setup in Visual Studio, `appsettings.json`
configuration, running tests, folder structure) and `DOCUMENTATION.md` (architecture, data
model, security model). This doc is the ClickUp-side project overview, not a replacement for
either.

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
   old list first or import into a fresh list, rather than ending up with duplicates.
6. Once imported, spot-check a couple of phases (Phase 11 and Phase 12 are the newest) to
   confirm dates, tags, and nesting landed as expected.
