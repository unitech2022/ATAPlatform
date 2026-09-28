# ATA — الموقع وبوابة السائق (website)

Public marketing site plus the driver document-upload portal, built with React 19, Vite, TypeScript, Tailwind CSS v4 and React Router v7. The visual language follows `docs/01-design-system.md` (tokens are declared in `src/index.css` under `@theme`).

## Requirements

- Node 22 and npm
- The backend API (`docs/05-api-contract.md`) running locally, or any reachable base URL

## Environment variables

Copy `.env.example` to `.env` and adjust as needed:

| Variable | Default | Purpose |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5000/api/v1` | Base URL of the ATA API (no trailing slash). |

## Run

```bash
npm install
npm run dev       # http://localhost:5173
npm run build     # type-check + production build into dist/
npm run preview   # serve the production build
npm run lint      # oxlint
```

## Routes

| Path | Page | Notes |
|---|---|---|
| `/` | Landing | Hero, ride categories (`GET /catalog/ride-categories` with static fallback when the API is down), safety, "انضم كسائق", store links, footer with support contacts. |
| `/driver` | Driver sign-in | Phone (+966, on-screen keypad or keyboard) → 4-digit OTP with resend countdown; the `devCode` hint is shown when the API returns it. Redirects to the portal when a session exists. |
| `/driver/portal` | Driver portal (protected) | Application number + status, 3-step progress (profile → vehicle → documents), forms, per-type document upload/preview/delete, "إرسال الطلب للمراجعة". After submission the page is read-only and polls `GET /driver/application` every 30 s. |

Any other path redirects to `/`.

## Language

Arabic (RTL) is the default. The AR/EN toggle in the header persists the choice in `localStorage` (`ata-language`), flips `<html lang dir>`, and is sent as `Accept-Language` on every request. All UI strings live in `src/i18n.ts`.

## Auth and API client

`src/lib/api.ts` wraps `fetch` with the base URL, `Accept-Language`, the bearer token from `localStorage` (`ata-session`), a single-flight refresh via `POST /auth/refresh` on `401`, and a typed `ApiError` mirroring the contract's `{ error: { code, message, details } }` envelope. File previews go through `GET /files/{id}` with the bearer token and are opened as object URLs.

## Project layout

```
src/
  App.tsx              routes
  main.tsx             providers (i18n, auth)
  i18n.ts              AR/EN dictionary + context
  index.css            Tailwind v4 @theme tokens and shared classes
  assets/logo.png
  components/          Icon, Button, Card, Keypad, OtpBoxes, StatusBadge, Field, FileDrop,
                       LanguageToggle, Header, Footer, Notice, ProgressSteps, MapArt, RequireAuth
  lib/                 api, types, session, auth context, errors, format, catalog, useResource
  pages/               Landing, DriverLogin, DriverPortal, portal/* sections
  providers/           I18nProvider, AuthProvider
```
