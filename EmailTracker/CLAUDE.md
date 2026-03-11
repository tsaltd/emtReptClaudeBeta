# EmailTracker CLAUDE.md

## Project Overview
ASP.NET Core 8 MVC web app for tracking email analysis data. Uses the MVVM-style pattern with Controllers, Services, Repositories, and ViewModels. Reads from an external SQLite database (Gmail analysis data).

## Tech Stack
- **Framework**: ASP.NET Core 8 MVC (`net8.0`)
- **ORM**: Entity Framework Core 8 with SQLite provider
- **JSON**: Newtonsoft.Json (`Microsoft.AspNetCore.Mvc.NewtonsoftJson`)
- **Database**: SQLite — path configured in `appsettings.json` → `ConnectionStrings:DefaultConnection`

## Project Structure
```
EmailTracker/
├── Controllers/        # MVC controllers (Controllers.cs)
├── Models/             # EF Core entity models (Run, Message, Sender, Rating, ViewEntities)
├── ViewModels/         # View-specific data models
├── Views/              # Razor views (Home, Run, Message, Sender, Shared)
├── Repositories/
│   ├── Interfaces/     # IRunRepository, IMessageRepository, ISenderRepository, IRatingRepository
│   └── Implementations/
├── Services/           # IRunService, IMessageService, ISenderService, IRatingService (Services.cs)
├── Data/               # AppDbContext
├── Program.cs          # App entry point, DI registration
├── appsettings.json    # Connection string and logging config
└── appsettings.Development.json
```

## Architecture
- **Pattern**: Repository + Service layer over EF Core
- **DI lifetime**: All repositories and services are `Scoped` (per-request)
- **Default route**: `{controller=Home}/{action=Index}/{id?}`

## Database
- SQLite database is **external** — not bundled with the project
- Connection string points to: `C:\Users\wp710\OneDrive\Dev\BetaLab\Gmail_Analysis_Solution\GmailAnalysisDB\db_runtime\gmailanalysis.db`
- To change DB location, update `appsettings.json` → `ConnectionStrings:DefaultConnection`

## Key Domain Models
- `Run` — represents an email analysis run/batch
- `Message` — individual email messages
- `Sender` — email senders
- `Rating` — ratings associated with messages or senders

## Build & Run
```bash
dotnet run
```
Runs on HTTPS with redirect. In development mode, no HSTS is applied.
