🇧🇷 [Versão em Português](README.pt-BR.md) | 🇺🇸 English

# Orçamento Familiar

> Family budget management web app — track income, fixed expenses, credit card bills, and monthly balance at a glance.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind%20CSS-3-06B6D4?logo=tailwindcss&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-22c55e)

Orçamento Familiar is a full-stack web application for tracking a family's finances month by month. It supports multiple family members through an invitation system, handles installment purchases with automatic propagation across future months, and allows importing credit card statements directly from PDF files.

---

## Features

### Annual Dashboard
- Table view: Income, Planned Expenses, Actual Expenses, Planned Balance, Actual Balance for every month
- Bar chart: Income vs Expenses
- Line chart: Balance trend over the year
- Year navigation

### Monthly View
- **Income** — primary and secondary salaries + extra income with full CRUD
- **Fixed Expenses** — full CRUD, automatically copied from the previous month
- **Credit Card Bills** — CRUD with filters by card/category, installment support with automatic propagation to future months, PDF statement import
- **Category Summary** — table + donut chart breakdown

### Cards
- Full CRUD for credit cards
- Usage tracking: current month spending vs monthly target per card

### Settings & Invitations
- View active members
- Manage pending invitations (copy link or revoke)
- Invite new members by email (7-day single-use links)

---

## Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| API | ASP.NET Core 8 (Clean Architecture) | REST API, auth |
| Auth | ASP.NET Core Identity + JWT | Access token in memory + refresh token in httpOnly cookie |
| ORM | Entity Framework Core 8 | Database access, migrations |
| Database | PostgreSQL 16 | Persistent storage |
| Frontend | React 18 + Vite + TypeScript | SPA |
| Styling | Tailwind CSS 3 | Utility-first CSS |
| Infra | Docker + Docker Compose | Container orchestration |

---

## Project Structure

```
/
├── backend/
│   ├── OrcamentoFamiliar.sln
│   ├── OrcamentoFamiliar.Domain/          # Entities, value objects
│   ├── OrcamentoFamiliar.Application/     # DTOs, interfaces, use cases
│   ├── OrcamentoFamiliar.Infrastructure/  # EF Core, DbContext, migrations, services, seeder
│   └── OrcamentoFamiliar.API/             # Controllers, Program.cs, Dockerfile
└── frontend/
    └── src/
        ├── api/         # Axios clients
        ├── components/  # Layout, modals (including PDF import)
        ├── contexts/    # AuthContext
        ├── hooks/       # Custom React hooks
        ├── pages/       # Dashboard, MonthlyView, Cards, Login, Register, Settings
        ├── types/       # TypeScript type definitions
        └── utils/
```

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running

---

## Quick Start (Docker Compose)

```bash
git clone https://github.com/codewiththiago/orcamento-familiar.git
cd orcamento-familiar

docker-compose up --build
```

Wait for the build (~3–5 min on first run). Then access:

| Service  | URL                            |
|----------|--------------------------------|
| Frontend | http://localhost               |
| API      | http://localhost:8080          |
| Swagger  | http://localhost:8080/swagger  |

### First Access

On the first run there are **no users**. Navigate to `/register` directly to create the initial account — no invitation required for the first user.

After that, new members must be invited via **Settings → Invite member**.

> The seeder creates default categories and cards automatically. No users are pre-created.

---

## Local Development (without Docker)

### Backend

Prerequisites: .NET 8 SDK and a running PostgreSQL instance.

```bash
cd backend

# Restore packages
dotnet restore OrcamentoFamiliar.sln

# Default connection string:
# Host=localhost;Port=5432;Database=orcamento_familiar;Username=postgres;Password=postgres
# Edit backend/OrcamentoFamiliar.API/appsettings.Development.json if needed

# Start the API (migrations and seed are applied automatically on startup)
cd OrcamentoFamiliar.API
dotnet run
# API available at: http://localhost:8080
```

### Frontend

Prerequisites: Node.js 20+

```bash
cd frontend

npm install

# Copy the environment file
cp .env.example .env.local
# VITE_API_URL=http://localhost:8080/api

npm run dev
# Available at: http://localhost:5173
```

---

## Environment Variables

### Backend

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | Secret key — random string, minimum 32 characters |
| `Jwt__Issuer` | `OrcamentoFamiliar` |
| `Jwt__Audience` | `OrcamentoFamiliarUsers` |
| `Frontend__Url` | Frontend URL (for CORS) |
| `ASPNETCORE_URLS` | `http://+:8080` |

### Frontend

| Variable | Description |
|----------|-------------|
| `VITE_API_URL` | Backend API URL (e.g. `http://localhost:8080/api`) |

---

## Invitation System

Any authenticated member can invite others:

1. Go to **Settings** in the sidebar
2. Enter the person's email and click **Invite**
3. The invitation link is generated and copied to clipboard automatically
4. Send the link via WhatsApp, email, etc.
5. The recipient opens the link → fills in name and password → enters the system

Invitations are valid for **7 days** and can only be used once.

---

## Migrations

Migrations are applied automatically when the backend starts. To create a new migration manually:

```bash
cd backend/OrcamentoFamiliar.API

dotnet ef migrations add MigrationName \
  --project ../OrcamentoFamiliar.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../OrcamentoFamiliar.Infrastructure \
  --startup-project .
```

---

## Deploy

### Railway

1. Create a project on [Railway](https://railway.app) and add a PostgreSQL service
2. Add two Deploy services pointing to this repository

**Backend**
- Root Directory: `backend`
- Dockerfile: `OrcamentoFamiliar.API/Dockerfile`
- Environment variables:
  ```
  ConnectionStrings__DefaultConnection=<PostgreSQL URL>
  Jwt__Key=<long random secret>
  Jwt__Issuer=OrcamentoFamiliar
  Jwt__Audience=OrcamentoFamiliarUsers
  Frontend__Url=<frontend URL>
  ASPNETCORE_URLS=http://+:8080
  ```

**Frontend**
- Root Directory: `frontend`
- Dockerfile: `frontend/Dockerfile`
- Environment variables:
  ```
  VITE_API_URL=<backend URL>/api
  ```
  > `VITE_API_URL` is injected at build time by Vite. After setting the variable, trigger a redeploy.

### Render

**Backend** — New Web Service
- Root: `backend`
- Build: `dotnet publish OrcamentoFamiliar.API/OrcamentoFamiliar.API.csproj -c Release -o out`
- Start: `dotnet out/OrcamentoFamiliar.API.dll`
- Environment variables: same as Railway above

**Frontend** — New Static Site
- Root: `frontend`
- Build: `npm install && npm run build`
- Publish directory: `dist`
- Rewrite rule: `/* → /index.html`

---

## Roadmap

- [ ] Mobile-responsive layout improvements
- [ ] Annual expense forecasts based on historical data
- [ ] Export budget summary to PDF / Excel
- [ ] Support for multiple currencies
- [ ] Savings goals tracking

---

## License

MIT © [codewiththiago](https://github.com/codewiththiago)
