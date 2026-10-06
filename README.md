# ARS Platform

Academic Research System — a full-stack web application for managing and reviewing academic research papers.

## Tech Stack

| Layer        | Technology                              |
| ------------ | --------------------------------------- |
| Frontend     | React 18 + TypeScript + Vite            |
| Backend      | ASP.NET Core 8 (Web API)                |
| Database     | Entity Framework Core + SQL Server       |
| Auth         | JWT Bearer Tokens                       |
| File Storage | Firebase Storage                        |
| PDF Viewer   | pdf.js (Mozilla)                        |
| PDF Editing  | pdf-lib                                 |

## Project Structure

```
ARSPlatform/
├── ARS_FE/                    # React frontend (Vite)
│   ├── src/
│   │   ├── components/        # Reusable UI components
│   │   ├── pages/             # Route-level page components
│   │   ├── layouts/           # Layout wrappers
│   │   ├── routes/            # React Router configuration
│   │   ├── services/          # API client (Axios)
│   │   ├── hooks/             # Custom React hooks
│   │   ├── store/             # Zustand state stores
│   │   ├── context/           # React Context providers
│   │   ├── utils/             # Helper functions
│   │   ├── config/            # App configuration
│   │   ├── types/             # TypeScript type definitions
│   │   ├── styles/            # Global styles & CSS modules
│   │   ├── lib/               # Third-party library setup
│   │   └── tests/             # Vitest tests
│   ├── .env.example           # Environment variable template
│   ├── TESTING.md             # Testing guide
│   └── package.json
│
├── ARSPlatform.API/           # ASP.NET Core Web API
├── ARSPlatform.MODEL/         # Domain models & DTOs
├── ARSPlatform.REPO/          # Repository pattern (data access)
├── ARSPlatform.SERVICE/       # Business logic services
│
├── Dockerfile                  # Docker container config
└── ARSPlatform.sln            # .NET solution file
```

## Prerequisites

- **Node.js** 18+
- **.NET SDK** 8.0+
- **SQL Server** (local or containerized)
- **Firebase project** (for file storage)

## Environment Setup

### Frontend

```bash
cd ARS_FE
cp .env.example .env
```

Edit `.env` and set your values:

```env
# API Base URL — point to your backend
VITE_API_BASE_URL=http://localhost:5000
```

### Firebase

The app uses Firebase Storage for PDF uploads. You need a Firebase project with:

1. **Authentication** → Enable Email/Password sign-in
2. **Storage** → Create a bucket and set rules to allow authenticated users
3. **Firestore** (optional) → For metadata

Fill in the `VITE_FIREBASE_*` variables in `.env` with your Firebase project config — get them from Firebase Console → Project Settings → Your apps → Web app → SDK setup and configuration. The `useFirebaseUpload` hook in `src/hooks/useFirebaseUpload.ts` reads these values automatically.

### Backend

Set up your `appsettings.json` inside `ARSPlatform.API/` with your connection string and JWT secret. Check `ARSPlatform.API/appsettings.json` for the expected keys.

## Running the Application

### 1. Start the Backend

```bash
# From the root directory
dotnet run --project ARSPlatform.API
```

The API will start at `http://localhost:5000` (or whatever port is configured in `appsettings.json`).

### 2. Start the Frontend

```bash
# In a new terminal
cd ARS_FE
npm install
npm run dev
```

The frontend dev server starts at `http://localhost:3000` and proxies `/api` requests to `http://localhost:5000`.

### 3. (Optional) Docker

Build and run everything in a container:

```bash
docker build -t ars-platform .
docker run -p 8080:8080 ars-platform
```

The Docker image only runs the backend. The frontend can still be served via `npm run dev` locally.

## npm Scripts (Frontend)

| Command                  | Description                                  |
| ------------------------ | -------------------------------------------- |
| `npm run dev`            | Start Vite dev server with HMR              |
| `npm run build`          | Type-check + production build                |
| `npm run preview`        | Preview the production build locally         |
| `npm run lint`           | Run ESLint                                   |
| `npm run test`           | Run all unit tests                           |
| `npm run test:integration`| Run integration tests only                   |
| `npm run test:coverage`   | Run all tests with coverage report           |
| `npm run test:ui`        | Run tests with interactive Vitest UI         |

> `npm run test` is safe to run at any time — it excludes Firebase integration tests so you won't be charged. Run `npm run test:integration` only when you need to test the upload/view pipeline. See `TESTING.md` for details.

## Default Test Account

| Field    | Value                       |
| -------- | --------------------------- |
| Email    | `admin@arsplatform.com`     |
| Password | `Password123`               |

(Configure this in the backend seed data / database migration.)

## Key Features

- **JWT Authentication** — login/register with protected routes
- **PDF Upload & Storage** — upload research papers to Firebase Storage
- **PDF Viewer** — render PDFs in-browser using pdf.js
- **PDF Figure Extraction** — extract figure images from uploaded PDFs using pdf-lib
- **Protected Pages** — authentication-gated routes
- **Form Validation** — Yup schemas with React Hook Form
