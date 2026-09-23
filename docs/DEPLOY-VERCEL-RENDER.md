# Deploy: Vercel (frontend) + Render (API)

Vercel cannot run ASP.NET Core, so we split the app:

- **API** (`HelpDesk.Api`) -> **Render**, as a Docker web service.
- **Frontend** (`src/HelpDesk.Api/wwwroot`) -> **Vercel**, as a static site.
- Vercel proxies every `/api/*` request to the Render API, so the browser
  talks to a single origin and there are no CORS problems.

```
Browser -> https://your-app.vercel.app  (static HTML/CSS/JS)
                    |
                    |  /api/*  (rewrite/proxy)
                    v
          https://helpdesk-api.onrender.com  (.NET API)
                    |
                    v
              SQLite / SQL Server
```

Files added for this setup:

- `Dockerfile` - builds and runs the API, listening on `$PORT`.
- `render.yaml` - Render Blueprint for the API web service.
- `vercel.json` - serves `wwwroot` and proxies `/api/*` to Render.

---

## Part A - Deploy the API on Render

### A1. Push the code to GitHub

```bash
git add .
git commit -m "chore: add Render and Vercel deployment configuration"
git push
```

### A2. Create the service

1. Sign in at https://render.com and authorize GitHub.
2. Click **New +** -> **Blueprint**.
3. Select the `helpdesk` repository.
4. Render detects `render.yaml`. Confirm the plan is **Free**.
5. Click **Apply**.

Render builds the `Dockerfile` and deploys. The first build takes a few minutes.

> Prefer manual setup? **New +** -> **Web Service** -> connect the repo ->
> Runtime **Docker** -> Plan **Free** -> Health Check Path `/api/health` ->
> add the environment variables from `render.yaml` manually.

### A3. Get the URL and verify

Once the status is **Live**, copy the URL, e.g. `https://helpdesk-api.onrender.com`.

```bash
curl https://helpdesk-api.onrender.com/api/health
# {"status":"healthy","service":"HelpDesk.Api","environment":"Production",...}

curl https://helpdesk-api.onrender.com/api/health/db
# {"database":"reachable","provider":"Microsoft.EntityFrameworkCore.Sqlite"}
```

Render also serves the frontend directly, so `https://helpdesk-api.onrender.com`
is already a fully working demo. Vercel is the nicer public URL.

### A4. Admin account

Seeded from `render.yaml`:

| Email                  | Password   |
|------------------------|------------|
| admin@helpdesk.local   | Admin@123  |

> Change `Seed__AdminPassword` in the Render dashboard before sharing the demo.

---

## Part B - Deploy the frontend on Vercel

### B1. Point the proxy at your API

Open `vercel.json` and replace the host with your real Render URL:

```json
{
  "source": "/api/:path*",
  "destination": "https://helpdesk-api.onrender.com/api/:path*"
}
```

Commit and push:

```bash
git add vercel.json
git commit -m "chore: point frontend proxy at the Render API"
git push
```

### B2. Import the project

1. Sign in at https://vercel.com with GitHub.
2. Click **Add New...** -> **Project** -> import the `helpdesk` repository.
3. Settings (they match `vercel.json`, so they are usually pre-filled):
   - **Framework Preset:** Other
   - **Root Directory:** repository root (leave empty)
   - **Build Command:** none / override off
   - **Output Directory:** `src/HelpDesk.Api/wwwroot`
   - **Install Command:** none / override off
4. Click **Deploy**.

### B3. Verify

1. Open the Vercel URL, e.g. `https://helpdesk.vercel.app`.
2. The page redirects to `login.html`.
3. Sign in with `admin@helpdesk.local` / `Admin@123`.
4. Create a ticket and watch the dashboard update.

`/api/health` on the Vercel domain is proxied, so this also works:

```bash
curl https://helpdesk.vercel.app/api/health
```

### B4. Lock down CORS (optional but recommended)

Browsers only talk to Vercel, and Vercel's server proxies to Render, so CORS is
not strictly required. If you want to call the Render URL directly from a
browser, set the allowed origin in Render:

1. Render dashboard -> `helpdesk-api` -> **Environment**.
2. Set `Cors__AllowedOrigins__0` to your Vercel URL, e.g.
   `https://helpdesk.vercel.app`.
3. **Save changes** (Render redeploys automatically).

---

## Part C - Vercel CLI alternative

If you prefer the command line:

```bash
# Install the CLI
npm install -g vercel

# Log in
vercel login

# Deploy a preview
vercel

# Deploy to production
vercel --prod
```

The CLI reads `vercel.json`, so the output directory and rewrite are applied
automatically.

---

## Part D - Make the database persistent

The Render **free** plan has an **ephemeral filesystem**: the SQLite file in
`/tmp` is recreated on every deploy/restart, so demo accounts and tickets reset.
Two options:

### Option 1 - Render persistent disk (paid instance)

1. Render dashboard -> `helpdesk-api` -> **Disks** -> **Add Disk**.
2. Name: `helpdesk-data`, Mount Path: `/var/data`, Size: `1 GB`.
3. Change the environment variable:

   ```text
   ConnectionStrings__SqliteConnection = Data Source=/var/data/helpdesk.db
   ```

4. Save and redeploy. Data now survives restarts.

### Option 2 - Managed SQL Server (closest to the resume story)

1. Provision Azure SQL, AWS RDS for SQL Server, or another SQL Server instance.
2. In Render set:

   ```text
   Database__Provider = SqlServer
   ConnectionStrings__DefaultConnection = Server=...;Database=HelpDeskDb;User Id=...;Password=...;TrustServerCertificate=True
   ```

3. Apply migrations once from your machine:

   ```bash
   export ConnectionStrings__DefaultConnection="<the same connection string>"
   dotnet ef database update --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.Api
   ```

> The app auto-migrates SQL Server on startup, so step 3 is only needed if you
> want to run migrations manually first.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Render build fails at `dotnet restore` | Network/NuGet hiccup | Retry the deploy; check the build log |
| Render deploy is Live but requests 502 | App not listening on `$PORT` | The Dockerfile binds to `$PORT`; confirm you did not hardcode 8080 |
| First request takes ~50s | Free services sleep after 15 min | Expected on the free plan; upgrade to keep it warm |
| Frontend loads but API calls 404 | `vercel.json` destination still points at the wrong host | Update the Render URL and redeploy |
| API calls return 401 right after login | `Jwt__Key` changed between requests | Set a fixed `Jwt__Key` instead of `generateValue` |
| Data disappears after a while | Ephemeral free filesystem | Use Part D |
| CORS error in the browser console | Calling Render directly instead of via Vercel | Use the Vercel URL, or set `Cors__AllowedOrigins__0` |

## Notes

- Vercel can technically run .NET through Docker container functions, but the
  function filesystem is read-only (only `/tmp` is writable) and each invocation
  is short-lived, so a self-contained SQLite database is not viable. A
  long-running Render web service is the simpler, correct fit here.
