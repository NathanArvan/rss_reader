# Personal RSS Reader

A curated, intentional RSS reader. Angular PWA frontend + ASP.NET Core Web API + SQLite.
See [docs/](docs/) for the design doc and technical roadmap.

## Stack

- **Frontend:** Angular 19 PWA (standalone components, SCSS)
- **Backend:** ASP.NET Core 8 Web API (minimal API)
- **Database:** SQLite via EF Core
- **Deploy:** Single deployable — the API serves the built Angular app as static files

## Layout

```
src/Api      ASP.NET Core Web API (serves the PWA in production)
src/Web      Angular PWA
tests        xUnit API tests
```

## Development

The API and Angular dev server run separately; Angular proxies `/api` to the API.

```bash
# Terminal 1 — API on http://localhost:5000
dotnet run --project src/Api

# Terminal 2 — Angular dev server (proxies /api -> :5000)
cd src/Web
npm install
npm start
```

Open the Angular dev server URL. The home page calls `/api/ping` and shows the result.

> Note: the PWA service worker is only active in a **production** build, not `ng serve`.
> To exercise PWA install/offline, use the production build or Docker (below).

## Production build (single deployable)

```bash
cd src/Web && npm install && npm run build   # outputs into ../Api/wwwroot
cd ../.. && dotnet publish src/Api -c Release -o publish
dotnet publish/RssReader.Api.dll              # serves the PWA + API on one port
```

## Docker

```bash
docker compose up --build
```

The SQLite database persists in a named volume.

## Tests

```bash
dotnet test                    # API (xUnit)
cd src/Web && npm test         # Angular (Karma/Jasmine, headless)
```

> The Angular tests launch headless Chrome. If Chrome isn't on the default path,
> point Karma at a Chromium binary, e.g. Edge:
> `set CHROME_BIN=C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`
