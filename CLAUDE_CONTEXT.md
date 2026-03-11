# CLAUDE_CONTEXT.md
# EmailTracker — Project Briefing for Claude
# paste this file into every new Claude chat session
# along with the GitHub repo URL for full context

---

## What This Project Is

An ASP.NET Core 8 MVC web application that browses and analyzes a batch of Gmail
email messages captured over 7-day windows. The core problem it solves is
**sender deduplication** — many emails disguise their true sender in the From
field display name, but share the same canonical email address.

Example:
```
"choking back tears…of JOY [Progressive Turnout Project]" <admin@e.turnoutpac.org>
"Social Security Poll (via Progressive Turnout Project)"  <admin@e.turnoutpac.org>
"MISSING: Important Data via TurnoutPAC.org"              <admin@e.turnoutpac.org>
```
All three are the same sender: `admin@e.turnoutpac.org`

---

## Database

- **Engine:** SQLite
- **File:** `gmailanalysis.db`
- **Location:** `C:\Users\wp710\OneDrive\Dev\BetaLab\Gmail_Analysis_Solution\GmailAnalysisDB\db_runtime\gmailanalysis.db`
- **Status:** Pre-existing — do NOT use EF Core migrations. Schema is source of truth.
- **Connection string is in:** `appsettings.json` and `appsettings.Development.json`

### Tables
```sql
rating    (rating_id PK, rating_name UNIQUE, sort_order UNIQUE)
run       (run_id PK, window_start, window_end, started_at, source_label)
sender    (sender_id PK, email_address UNIQUE, display_name, first_seen,
           last_seen, msg_count, rating_id FK→rating DEFAULT 3, created_at, updated_at)
message   (message_id PK, run_id FK→run, sender_id FK→sender,
           gmail_message_id, thread_id, internal_date, header_date,
           subject, snippet, from_raw, to_raw, created_at)
```

### Views (keyless in EF Core)
```sql
v_sender_with_rating    -- sender joined to rating
v_message_with_sender   -- message joined to sender and rating
```

### Important
- `rating_id = 3` is ORANGE — the default for all new senders
- `msg_count` on sender is maintained manually (no auto-trigger)
- All dates are TEXT in ISO-8601 format
- Trigger `trg_sender_updated_at` fires on sender UPDATE

---

## Architecture — MVVM Pattern

```
Models/               ← EF Core entities ONLY. Never used directly in Views.
  Rating.cs
  Run.cs
  Sender.cs
  Message.cs
  ViewEntities.cs     ← Keyless: VSenderWithRating, VMessageWithSender

ViewModels/           ← All view logic, computed properties, search state
  ViewModels.cs       ← All VMs in one file

Data/
  AppDbContext.cs     ← EF Core DbContext, maps tables + views

Repositories/
  Interfaces/
    IRepositories.cs  ← IRunRepository, IMessageRepository,
                         ISenderRepository, IRatingRepository
  Implementations/
    Repositories.cs   ← All concrete implementations

Services/
  Services.cs         ← All interfaces + implementations:
                         IRunService / RunService
                         IMessageService / MessageService
                         ISenderService / SenderService  ← includes ExtractCanonicalEmail()
                         IRatingService / RatingService

Controllers/
  Controllers.cs      ← All controllers in one file (THIN — orchestrate only):
                         HomeController, RunController,
                         MessageController, SenderController

Views/
  Home/Index.cshtml           ← Dashboard
  Run/Index.cshtml            ← Run list (AJAX search)
  Run/_RunRows.cshtml         ← AJAX partial
  Run/Detail.cshtml           ← Run detail + top senders
  Message/Index.cshtml        ← Message search (AJAX filter bar)
  Message/_MessageRows.cshtml ← AJAX partial
  Sender/Index.cshtml         ← Sender list (AJAX search + rating filter)
  Sender/_SenderRows.cshtml   ← AJAX partial
  Sender/Detail.cshtml        ← Sender profile + recent messages
  Sender/Browse.cshtml        ← Single-record browser with NEXT/PREV navigation
  Sender/_BrowseCard.cshtml   ← AJAX partial swapped by nav buttons
  Shared/_Layout.cshtml       ← Sidebar layout (Bootstrap 5 + jQuery)
```

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 + Microsoft.EntityFrameworkCore.Sqlite |
| Frontend | Razor Views + Bootstrap 5 + jQuery AJAX |
| Database | SQLite (pre-existing, read/write) |
| IDE | Visual Studio 2022 Community Edition |
| Source Control | GitHub (via GitHub Desktop) |
| JSON | Newtonsoft.Json (Microsoft.AspNetCore.Mvc.NewtonsoftJson) |

**React is NOT used. No frontend frameworks. Bootstrap + jQuery only.**

---

## Key Coding Conventions

1. **Controllers are thin** — 10-15 lines per action max. No business logic.
2. **Views only bind to ViewModels** — Models never passed directly to Views.
3. **AJAX pattern** — full page on first load, partial view swaps on filter/nav.
4. **Partial views** named with underscore prefix: `_RunRows.cshtml` etc.
5. **No inline C# ternary in `<option>` tag attributes** — causes RZ1031 error.
   Use `Html.Raw()` with StringBuilder instead:
   ```csharp
   @{
       var opts = new System.Text.StringBuilder();
       opts.Append("<option value=\"\">All</option>");
       foreach (var r in Model.AvailableRatings)
       {
           var sel = (Model.RatingFilter == r.RatingName) ? " selected" : "";
           opts.Append($"<option value=\"{r.RatingName}\"{sel}>{r.RatingName}</option>");
       }
   }
   <select id="s-rating" class="form-select form-select-sm">
       @Html.Raw(opts.ToString())
   </select>
   ```
6. **All dates are TEXT** in SQLite — use `string` in C# models, parse for display in ViewModels.
7. **AppDbContext registered as Scoped** — fresh connection per HTTP request.

---

## Browse Sender Feature

**URL:** `/Sender/Browse?index=0`

- Sorted by `msg_count DESC` (highest volume senders first)
- Single record view: email address, msg count, rating dropdown
- NEXT / PREV buttons (±1) and NEXT 20 / PREV 20 buttons (±20)
- Jump-to input for direct navigation
- Rating dropdown saves **immediately on change** via AJAX POST
- URL updates via `history.pushState` — browser back/forward works
- CSRF protected via `[ValidateAntiForgeryToken]`

**Controller actions:**
```
GET  /Sender/Browse?index=0&partial=false  → full page
GET  /Sender/Browse?index=0&partial=true   → partial (AJAX nav)
POST /Sender/UpdateRating                  → { senderId, ratingId } → JSON response
```

---

## Sender Canonicalization

`SenderService.ExtractCanonicalEmail(fromRaw)` extracts the true email from raw From headers:
```
"Display Name [Org]" <user@domain.com>  →  user@domain.com
user@domain.com                          →  user@domain.com
```
Looks for last `<...>` pair, falls back to full string if it contains `@`. Always lowercased.

---

## Connection String

```json
"DefaultConnection": "Data Source=C:\\Users\\wp710\\OneDrive\\Dev\\BetaLab\\Gmail_Analysis_Solution\\GmailAnalysisDB\\db_runtime\\gmailanalysis.db"
```
Set in both `appsettings.json` and `appsettings.Development.json`.
`Program.cs` reads it with a matching hardcoded fallback. No environment variable expansion.

---

## NuGet Packages

```xml
Microsoft.EntityFrameworkCore                  8.0.0
Microsoft.EntityFrameworkCore.Sqlite           8.0.0
Microsoft.EntityFrameworkCore.Design           8.0.0
Microsoft.AspNetCore.Mvc.NewtonsoftJson        8.0.0
```

---

## Known Issues / QA Notes

- RZ1031 warning fixed: `<option>` tags now use `Html.Raw()` + StringBuilder pattern
- EF Core migrations: NOT used — database is pre-existing
- `msg_count` on sender must be maintained by the ingestion service, not the web app

---

## What Was Built in Session 1

- [x] Full MVVM project scaffold
- [x] All 4 EF Core entity models
- [x] Keyless view entities for SQLite views
- [x] AppDbContext with full model configuration
- [x] Repository interfaces + implementations (all 4)
- [x] Service layer (all 4 services)
- [x] Thin controllers (Home, Run, Message, Sender)
- [x] Bootstrap 5 sidebar layout
- [x] Dashboard view
- [x] Run list + detail views (AJAX)
- [x] Message search view with filter bar (AJAX)
- [x] Sender list view (AJAX)
- [x] Sender detail view
- [x] Browse Sender — single record navigator with NEXT/PREV/NEXT20/PREV20
- [x] Rating dropdown — immediate save via AJAX POST
- [x] Connection string pointing to gmailanalysis.db
- [x] seed.sql — 23 senders, 3 runs, ~60 messages
- [x] .gitignore
- [x] CLAUDE_CONTEXT.md (this file)

## Next Phase (To Be Determined After QA)

- [ ] QA results from wp710 — forms, navigation, data display
- [ ] Additional forms TBD
- [ ] Integration testing against real gmailanalysis.db data
- [ ] Hosting / deployment configuration

---

## How to Start a New Claude Chat

1. Open this file
2. Say: *"I am continuing development on the EmailTracker project. Here is my context document."*
3. Paste or upload `CLAUDE_CONTEXT.md`
4. Paste your GitHub repo URL
5. Describe what you want to work on next

Claude will have full context and can pick up exactly where we left off.
