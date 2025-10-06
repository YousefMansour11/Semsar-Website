# Semsar — Real Estate Marketplace

Semsar is a full-stack real estate platform connecting buyers with verified
properties and projects in Egypt. It consists of a public-facing website
(`frontend/semsar-web`), an admin dashboard (`frontend/semsar-admin`), and a
.NET 10 REST API backend (`backend/Semsar-Backend`).

## Repo layout

```
backend/Semsar-Backend/   .NET 10 solution (API + Application + Domain + Infrastructure + tests)
frontend/semsar-admin/    Admin dashboard (React + Vite + Tailwind + Radix/shadcn)
frontend/semsar-web/      Public website (React + Vite + Tailwind + Radix/shadcn)
```

## Backend

- Clean Architecture: `Domain` → `Application` → `Infrastructure` → `API`
- Entity Framework Core (SQL Server) with migrations under
  `Infrastructure/Migrations`
- Redis-backed caching, rate limiting, bot detection, and distributed stores
- Cloudinary for image/video upload and delivery
- JWT authentication and refresh tokens
- Hangfire background jobs (SEO recalc, sitemap generation, upload cleanup,
  reservation cleanup)
- SEO pipeline: canonical tags, Open Graph, JSON-LD, sitemap, ranking feedback
  loops, and crawl-budget controls
- CI/CD via GitHub Actions (`.github/workflows/ci-cd.yml`) publishing over FTP
  to `https://semsar-hub.runasp.net`

### Building the API

Requires .NET 10 SDK:

```sh
dotnet restore backend/Semsar-Backend/RealEstate.slnx
dotnet build backend/Semsar-Backend/RealEstate.slnx
```

Configuration is supplied at deploy time via environment variables / secrets
(`ConnectionStrings`, `Jwt__Key`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`,
`Smtp__*`). See `backend/Semsar-Backend/API/appsettings.Production.json`.

## Frontends

Both apps are Vite + React 18 + TypeScript + Tailwind CSS. Radix UI (shadcn/ui)
components provide the base UI kit.

```sh
cd frontend/semsar-web    # or frontend/semsar-admin
npm install
npm run dev               # local dev server
npm run build             # production build
npm run lint              # eslint
npm run typecheck         # tsc --noEmit
npm test                  # vitest
```

The public web app is deployed to Vercel (`semsar.vercel.app`); the admin app
deploys to Vercel under the same account. API base URL comes from `VITE_API_BASE`.

## License

Private repository. All rights reserved.