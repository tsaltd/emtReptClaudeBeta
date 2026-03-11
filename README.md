# EmailTracker — ASP.NET Core MVC + SQLite (MVVM)

## Prerequisites
- Visual Studio 2022 Community Edition
- .NET 8 SDK
- Your existing `emailtracker.db` SQLite file

## Setup Steps

### 1. Open the Solution
Open `EmailTracker.sln` in Visual Studio.

### 2. Restore NuGet Packages
Visual Studio will restore automatically, or run:
```
dotnet restore
```

### 3. Point to Your SQLite Database
In `appsettings.json`, update the connection string to point to your `.db` file:
```json
"DefaultConnection": "Data Source=C:\\path\\to\\your\\emailtracker.db"
```
Or copy `emailtracker.db` into the `EmailTracker/` project folder (next to `Program.cs`).

### 4. Run
Press **F5** or `Ctrl+F5` in Visual Studio.

---

## Architecture (MVVM)

```
Models/           ← EF Core entities only — never used directly in Views
  Rating.cs
  Run.cs
  Sender.cs
  Message.cs
  ViewEntities.cs ← Keyless entities for SQLite views (v_sender_with_rating, v_message_with_sender)

ViewModels/       ← All view logic, display properties, computed fields, search state
  ViewModels.cs

Data/
  AppDbContext.cs ← EF Core context with table + view mappings

Repositories/
  Interfaces/     ← IRunRepository, IMessageRepository, ISenderRepository, IRatingRepository
  Implementations/← Concrete EF Core implementations with query logic

Services/         ← Business logic: sender normalization, VM mapping, aggregation
  Services.cs     ← RunService, MessageService, SenderService, RatingService

Controllers/      ← THIN: only orchestrate ViewModel ↔ Service ↔ View
  Controllers.cs  ← HomeController, RunController, MessageController, SenderController

Views/
  Home/Index.cshtml          ← Dashboard
  Run/Index.cshtml           ← Run list (AJAX search)
  Run/_RunRows.cshtml        ← AJAX partial
  Run/Detail.cshtml          ← Run detail + top senders
  Message/Index.cshtml       ← Message search (AJAX filter bar)
  Message/_MessageRows.cshtml← AJAX partial
  Sender/Index.cshtml        ← Sender list (AJAX search + rating filter)
  Sender/_SenderRows.cshtml  ← AJAX partial
  Sender/Detail.cshtml       ← Sender profile + recent messages
  Shared/_Layout.cshtml      ← Sidebar layout
```

## Key Design Decisions

| Decision | Reason |
|----------|--------|
| Views **only** bind to ViewModels | True MVVM — Model never leaks to View |
| Controllers are thin (≤ 15 lines each action) | Orchestration only, no business logic |
| SQLite views mapped as Keyless entities | Leverages your `v_sender_with_rating` and `v_message_with_sender` |
| AJAX partials (`_XxxRows.cshtml`) | No full-page reloads on search/filter |
| `ExtractCanonicalEmail()` in SenderService | Core business logic for sender deduplication |

## Sender Canonicalization
The `SenderService.ExtractCanonicalEmail()` method handles formats like:
```
"choking back tears [Progressive Turnout Project]" <admin@e.turnoutpac.org>
→ admin@e.turnoutpac.org
```

## Hosting
For IIS deployment:
1. Publish: `dotnet publish -c Release`
2. Copy published output to your IIS site folder
3. Ensure the app pool uses **No Managed Code** (Kestrel handles .NET)
4. Make sure the SQLite `.db` file path is accessible by the IIS app pool identity
