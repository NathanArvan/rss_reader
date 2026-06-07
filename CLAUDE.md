# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A personal, single-user RSS reader built around *intentional* consumption — the opposite of an algorithmic feed. Product decisions in [docs/rss-reader-design.md](docs/rss-reader-design.md) deliberately reject conventional "good UX": **no unread counts by default, no infinite scroll, no recommendations, no engagement metrics, mark-all-as-read with no confirmation.** When a feature seems to be missing (e.g. unread badges), check the design doc before "fixing" it — the absence is usually intentional. [docs/rss-reader-technical-roadmap.md](docs/rss-reader-technical-roadmap.md) defines the phased build order; code comments reference these phase/step numbers (e.g. `[fwd: 3]`, "Phase 1, Step 2").

## Commands

Backend (.NET 8, run from repo root):
```bash
dotnet run --project src/Api          # API on http://localhost:5000
dotnet test                           # all xUnit tests
dotnet test --filter "FullyQualifiedName~FeedFetcherTests"   # single class
dotnet test --filter "FullyQualifiedName~FeedFetcherTests.DeduplicatesByGuid"  # single test
```

Frontend (Angular 19, run from `src/Web/`):
```bash
npm install
npm start                             # dev server, proxies /api -> :5000
npm test                              # Karma/Jasmine, headless Chrome
npm run build                         # production build -> ../Api/wwwroot
```

The Angular tests need a Chrome/Chromium binary. If none is on the path, set `CHROME_BIN`, e.g. `set CHROME_BIN=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`.

Full single deployable (API serves the built PWA on one port):
```bash
docker compose up --build             # http://localhost:8080, SQLite in a named volume
```

## Architecture

**Single-deployable design.** In production there is *one* process: the ASP.NET Core API serves both `/api/*` and the compiled Angular PWA as static files from `wwwroot`. `angular.json` is configured to emit its build directly into `../Api/wwwroot`, and the Dockerfile relies on this (web-build stage → copied into the API publish stage). `Program.cs` ends with `UseDefaultFiles`/`UseStaticFiles`/`MapFallbackToFile("index.html")` so client-side routes fall through to the SPA. In development the two run separately and Angular's `proxy.conf.json` forwards `/api` to `:5000`.

**Backend (`src/Api`)** — minimal API, no controllers.
- `Program.cs` is the composition root: DI registration, EF migration-on-startup (`db.Database.Migrate()` runs every boot, so the SQLite file and schema are created automatically — there is no separate migration step to run), and `/api/ping` health check.
- Endpoints are extension methods grouped by resource (`Endpoints/SourceEndpoints.cs`, `Endpoints/ItemEndpoints.cs`), registered via `MapSourceEndpoints()` / `MapItemEndpoints()`. Request/response DTOs are `record`s defined in the same file as their endpoints; map domain models to DTOs with the static `From(...)` factory on each response record — the DTO layer is where "what the design doc allows the client to see" is enforced (e.g. no unread counts).
- `Services/FeedFetcher.cs` (`IFeedFetcher`) does one source's fetch: conditional GET via stored `ETag`/`LastModified`, parse with `CodeHollow.FeedReader`, **dedup by GUID and fall back to URL**, and record `LastError`/`LastErrorUtc` on failure rather than throwing. `Services/FeedFetchingService.cs` is the `BackgroundService` that polls all sources on a loop (interval from config `FeedFetching:IntervalMinutes`, default 60). The background service creates its own DI scope per cycle because `FeedFetcher` and `AppDbContext` are scoped.
- `Data/AppDbContext.cs` owns the schema (fluent config in `OnModelCreating`: unique index on `FeedUrl`, cascade delete of items with their source, `(SourceId, Guid)` index for dedup). EF migrations live in `Migrations/`.
- `Models/` — `Source` and `Item` carry forward-looking columns for not-yet-built phases (marked `[fwd: N]`) so future steps don't need schema migrations.

**Frontend (`src/Web`)** — standalone components (no NgModules), lazy-loaded routes in `app.routes.ts`, SCSS, PWA via `@angular/service-worker`.
- `services/*.service.ts` wrap the API. Notably there is **no backend "all items" endpoint**: `ItemsService.listAll()` fetches every source and merges/sorts their items client-side. If you add a combined-feed endpoint server-side, this is the code to replace.
- `models/api.models.ts` is the hand-maintained TypeScript mirror of the backend DTOs (camelCase JSON). When you change a DTO in `Endpoints/*.cs`, update this file to match — `SourceType` enum values must stay aligned with `Models/SourceType.cs`.

> The PWA service worker only activates in a **production** build, not `ng serve`. Test install/offline via `npm run build` or Docker.

## Testing

API tests (`tests/RssReader.Api.Tests`) use `TestWebApplicationFactory`, which boots the real app against a unique temp SQLite file per instance (migrations applied on startup) and exposes `SeedAsync(...)` for arranging data. Tests are integration-style against the running app, not isolated unit tests. Frontend specs are `*.spec.ts` colocated with components/services.
