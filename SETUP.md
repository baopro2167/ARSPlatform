# ARSPlatform — Local Setup Guide

Welcome to the **Academic Research System (ARS)** project. This guide walks a new team member through getting the full stack running locally: React (Vite) frontend + ASP.NET Core 8 Web API + SQL Server + Firebase Storage.

> If anything is unclear or out of date, ping the team in your onboarding channel.

---

## 1. Prerequisites

Make sure these are installed on your machine before continuing.

| Tool | Version | Notes |
|------|---------|-------|
| **Node.js** | 18+ | Includes `npm`. LTS recommended. |
| **.NET SDK** | 8.0+ | Verify with `dotnet --version`. |
| **SQL Server** | 2019+ or SQL Server Express / LocalDB | LocalDB ships with Visual Studio. |
| **Git** | latest | For cloning the repo. |
| **Firebase account** | free tier is fine | Needed for Auth + Storage. |

Verify your environment:

```bash
node --version     # expect v18.x or newer
npm --version
dotnet --version   # expect 8.x.x
sqlcmd -S . -E     # optional — confirms local SQL Server is reachable
```

---

## 2. Clone the Repository

```bash
git clone <YOUR_REPO_URL> ARSPlatform
cd ARSPlatform
```

The repo is a Visual Studio solution at the root (`ARSPlatform.sln`) plus a `ARS_FE/` folder for the React app.

---

## 3. Frontend Setup

### 3.1 Install dependencies & copy env template

```bash
cd ARS_FE
npm install
cp .env.example .env       # Windows PowerShell: copy .env.example .env
```

Open `ARS_FE/.env`. You'll fill in five sections:

### 3.2 Create a Firebase project

1. Go to the [Firebase Console](https://console.firebase.google.com/).
2. Click **Add project** → name it (e.g. `ars-platform-dev`) → continue (disable Google Analytics if you don't need it).
3. In the left sidebar, go to **Authentication** → **Sign-in method** → enable **Email/Password**.
4. In the left sidebar, go to **Storage** → click **Get started** → choose your region → enable.
5. (Optional) Enable **Firestore** if you want server-side metadata sync.
6. In **Project Settings** (gear icon) → **Your apps** → click the web icon `</>` to register a web app.
7. Copy the config object values into `.env`:

| `.env` variable | Where it comes from |
|-----------------|---------------------|
| `VITE_FIREBASE_API_KEY` | `apiKey` |
| `VITE_FIREBASE_AUTH_DOMAIN` | `authDomain` (e.g. `your-project.firebaseapp.com`) |
| `VITE_FIREBASE_PROJECT_ID` | `projectId` |
| `VITE_FIREBASE_STORAGE_BUCKET` | `storageBucket` (e.g. `your-project.appspot.com`) |
| `VITE_FIREBASE_MESSAGING_SENDER_ID` | `messagingSenderId` |
| `VITE_FIREBASE_APP_ID` | `appId` |
| `VITE_FIREBASE_MEASUREMENT_ID` | `measurementId` (optional) |

8. Set the **API base URL** — point this at your local backend (HTTPS profile uses 5001, HTTP uses 5000):

```env
VITE_API_BASE_URL=https://localhost:5001
```

> **Heads-up:** the dev server (`npm run dev`) proxies `/api` calls to whichever URL is in `VITE_API_BASE_URL`. If the backend isn't running, requests will fail.

---

## 4. Backend Setup

The backend lives in `ARSPlatform.API/` and reads its config from `appsettings.json`.

### 4.1 Update the SQL Server connection string

Open `ARSPlatform.API/appsettings.json` and edit `ConnectionStrings.DefaultConnection`:

**LocalDB (easiest, ships with Visual Studio):**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ARSFlatformDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

**Full SQL Server:**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ARSFlatformDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### 4.2 Generate a JWT signing key

The JWT key must be **at least 32 bytes (256 bits)**. Pick any of these:

**PowerShell:**

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

**Node.js:**

```bash
node -e "console.log(require('crypto').randomBytes(48).toString('base64'))"
```

**.NET CLI:**

```bash
dotnet user-jwts key
```

> **Never commit a real production key.** For local dev the bundled default is fine, but if you change it, also update `Issuer` / `Audience` if your team uses shared values.

### 4.3 Restore NuGet packages

```bash
cd ../
dotnet restore ARSPlatform.sln
```

---

## 5. Database Setup

The app calls `context.Database.EnsureCreated()` at startup, which creates the schema and seeds data on first run — no manual migration step required.

Just make sure the connection string in `appsettings.json` points at a server your account can write to, then run the API once (next step) — tables and seed users will be created automatically.

> If you'd rather use EF Core migrations later (recommended for production): `dotnet ef migrations add InitialCreate --project ARSPlatform.REPO --startup-project ARSPlatform.API` then `dotnet ef database update`.

---

## 6. Running the App

Open **two terminals** — one for the backend, one for the frontend.

### 6.1 Start the backend

From the repo root:

```bash
dotnet run --project ARSPlatform.API
```

The API listens on:

- HTTPS: `https://localhost:5001`
- HTTP:  `http://localhost:5000`

Swagger UI is available at `https://localhost:5001/swagger` while running in Development.

### 6.2 Start the frontend

In a separate terminal:

```bash
cd ARS_FE
npm run dev
```

Open `http://localhost:3000` in your browser.

### 6.3 Default test account

The seed data inserts one admin account you can use to log in immediately:

| Field    | Value                   |
|----------|-------------------------|
| Email    | `admin@arsplatform.com` |
| Password | `Password123`           |

---

## 7. Troubleshooting

| Issue | Fix |
|-------|-----|
| **CORS error in browser console** | The API uses an `AllowAll` CORS policy by default. If you've changed it in `Program.cs`, make sure `http://localhost:3000` is listed under `AllowedOrigins`. |
| **`Invalid JWT Key` / `IDX10720`** | Your `JwtSettings.Key` is shorter than 32 bytes. Generate a new one (see §4.2). |
| **`Cannot open database "ARSFlatformDb"` requested by the login** | The database doesn't exist yet — that's normal on first run. `EnsureCreated()` will create it; if it failed, check SQL Server is running and your account has `dbcreator` rights. |
| **`A connection was successfully established… but login failed`** | Wrong SQL username/password. Verify with `sqlcmd -S localhost -U sa -P YOUR_PASSWORD`. |
| **Firebase uploads fail with `auth/unauthorized`** | Check Firebase Storage rules allow authenticated users. For local dev, set: `allow read, write: if request.auth != null;` |
| **Firebase uploads fail with `storage/unauthorized`** | Verify `VITE_FIREBASE_*` values in `.env` are correct and that the Storage bucket is enabled in the Firebase Console. |
| **`Cannot find module 'firebase/...'`** | Run `npm install` again, or `npm install firebase` if you've pruned `node_modules`. |
| **Frontend shows "Network Error"** | Backend isn't running, or `VITE_API_BASE_URL` is wrong. Re-check `.env` then restart `npm run dev`. |
| **HTTPS cert warning on localhost** | Click "Advanced" → "Proceed to localhost" in the browser, or trust the dev cert: `dotnet dev-certs https --trust`. |
| **`dotnet ef` command not found** | Run `dotnet tool install --global dotnet-ef` then `export PATH="$PATH:$HOME/.dotnet/tools"` (PowerShell: add `$HOME\.dotnet\tools` to `$env:PATH`). |

---

## 8. Useful npm scripts (frontend)

| Command | What it does |
|---------|--------------|
| `npm run dev` | Start Vite dev server with HMR |
| `npm run build` | Type-check + production build into `dist/` |
| `npm run preview` | Serve the built `dist/` locally |
| `npm run lint` | Run ESLint |
| `npm run test` | Run all Vitest unit tests |
| `npm run test:integration` | Run integration tests (touches real Firebase) |
| `npm run test:coverage` | Run all tests with coverage report |
| `npm run test:ui` | Open the Vitest UI in a browser |

See `ARS_FE/TESTING.md` for the full testing guide.

---

## 9. Project Structure (quick map)

```
ARSPlatform/
├── ARS_FE/                    # React + Vite + TypeScript frontend
├── ARSPlatform.API/           # ASP.NET Core Web API (controllers, Program.cs)
├── ARSPlatform.MODEL/         # Domain entities + DTOs
├── ARSPlatform.REPO/          # Repository pattern + EF Core DbContext
├── ARSPlatform.SERVICE/       # Business logic / external services
├── Dockerfile                 # Optional container for the API
└── ARSPlatform.sln
```

---

**You're all set.** Hit `http://localhost:3000`, log in with `admin@arsplatform.com / Password123`, and start exploring.
