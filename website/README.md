# ATA — الموقع وبوابة السائق وبوابة الشركات (website)

Public marketing site, the driver document-upload portal, the public trip-tracking page, the help center, legal pages and the corporate portal (`/business`), built with React 19, Vite, TypeScript, Tailwind CSS v4 and React Router v7. The visual language follows `docs/01-design-system.md` (tokens are declared in `src/index.css` under `@theme`).

## Requirements

- Node 22 and npm
- The backend API (`docs/05-api-contract.md`) running locally, or any reachable base URL

## Environment variables

Copy `.env.example` to `.env` and adjust as needed:

| Variable | Default | Purpose |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5000/api/v1` | Base URL of the ATA API (no trailing slash). |
| `VITE_BUSINESS_EMAIL` | `business@ata.sa` | Sales mailbox for the `/business` "talk to sales" form (mailto, no API). |
| `VITE_BUSINESS_WHATSAPP` | _(empty)_ | WhatsApp number (international, digits only); the WhatsApp link is hidden when empty. |
| `VITE_GEOCODER_URL` | `https://nominatim.openstreetmap.org` | Place search / reverse geocoding on the corporate booking map. Set to an empty string to disable (points are then picked by clicking the map). |

### Runtime configuration (Docker)

`index.html` loads `/config.js` before the app. In the Docker image, `docker/40-config.sh` (run by the nginx entrypoint) rewrites it as `window.__ATA_CONFIG__ = { apiBaseUrl, hubUrl, oneSignalAppId }` from `ATA_API_BASE_URL`, `ATA_HUB_URL`, `ATA_ONESIGNAL_APP_ID`. `src/lib/config.ts` reads it first and falls back to `VITE_API_BASE_URL`. `public/config.js` is an empty placeholder for `npm run dev`.

```bash
docker build -t ata-website .
docker run -p 8080:80 -e ATA_API_BASE_URL=https://api.ata.localhost/api/v1 ata-website
```

nginx (`docker/nginx.conf`) serves `dist/` with SPA fallback, immutable caching for `/assets/`, `no-store` for `/config.js` and `X-Robots-Tag: noindex` for `/t/*`.

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
| `/` | Landing | Hero, ride categories (`GET /catalog/ride-categories` with static fallback when the API is down), safety, "انضم كسائق", store links. Header nav includes Help and Business; the footer links Help, Business, Privacy and Terms. |
| `/driver` | Driver sign-in | Phone (+966, on-screen keypad or keyboard) → 4-digit OTP with resend countdown; the `devCode` hint is shown when the API returns it. Redirects to the portal when a session exists. |
| `/driver/portal` | Driver portal (protected) | Application number + status, 3-step progress (profile → vehicle → documents), forms, per-type document upload/preview/delete, "إرسال الطلب للمراجعة". After submission the page is read-only and polls `GET /driver/application` every 30 s. |
| `/t/:token` | Public trip tracking (F12) | No nav (logo + language only), `noindex`. Polls `GET /public/trip-shares/{token}` every `refreshSeconds` (min 5 s, backs off on 429/network errors, pauses while the tab is hidden, stops when the trip ends). Leaflet/OSM map with the planned route (dashed brand), travelled route (ink), pickup/stops/dropoff pins and the driver marker rotated to `heading`; status card, ETA to pickup/destination, driver (first name, photo, rating), vehicle (plate `dir=ltr`), route. `410` → "انتهت صلاحية رابط التتبع", `404` → "الرابط غير صحيح". |
| `/help` | Help center (F18) | Search (debounced, mirrored to `?q=`), passenger/driver tabs (`?audience=`), categories (`GET /help/categories`), articles (`GET /help/articles`), and the "didn't find your answer? open a ticket in the app" CTA with store links and support contacts. |
| `/help/categories/:categoryId` | Help category | Articles of one category, paginated. |
| `/help/:slug` | Help article | Markdown via `react-markdown` + `rehype-sanitize`, breadcrumbs, tags, related articles, "هل كان المقال مفيداً؟" (`POST /help/articles/{id}/feedback`, remembered per browser), per-article `<title>` and meta description. |
| `/privacy` · `/terms` | Legal pages | Privacy policy (PDPL: data collected, purposes, sharing, retention, rights, how to download your data and delete your account in the app — 24 h / 7-day export, 30-day deletion grace and blockers) and terms of use. Bilingual content in `src/content/legal.ts`; wording pending legal review (docs/12 §F21.12). |
| `/business` | ATA for Business landing (F19) | Benefits, how it works, "دخول الشركات", and a "talk to sales" form that opens a prefilled `mailto:` (plus WhatsApp when configured) — no API. |
| `/business/login` | Corporate admin sign-in | Same phone/OTP flow with `role: "corporate_admin"`; rejects sessions without the `corporate_admin` role; returns to the page that required sign-in. |
| `/business/join/:token` | Invitation landing | Instructions for employees (app, same number, accept in Account → Company account) and admins (sign in to the portal). The token is never sent anywhere. |
| `/business/app` | Company overview (protected) | `GET /corporate/dashboard`: month-to-date trips/spend, employees, open invoices, credit and budget meters, recent trips. |
| `/business/app/bookings` · `/new` · `/:tripId` | Bookings | List with status/rider/date filters; new booking for an employee or a guest (Leaflet click/drag point picking + place search, category, now/scheduled within 7 days, purpose, cost center) → `POST /corporate/bookings/quote` shows price, ETA, policy result and remaining budget → `POST /corporate/bookings`; detail with live polling (10 s), map, driver/vehicle, timeline and cancellation with reason + fee preview. |
| `/business/app/employees` · `/:id` | Employees | Search/status/cost-center filters, invite (modal), CSV import with template download and skipped-rows report, detail/edit, disable/enable, resend or cancel invitation. |
| `/business/app/policies` | Policies | Cards + editor: categories, days, time windows, fare cap, monthly budget, purpose/cost-center requirements, scheduled/guest toggles, active; set default, delete. |
| `/business/app/cost-centers` | Cost centers | CRUD. |
| `/business/app/invoices` · `/:id` | Invoices | List with status filter and PDF download; detail with VAT totals, paginated lines, PDF and CSV download (authenticated blob downloads). |
| `/business/app/reports` | Reports | Date range + group by (employee/department/cost center/month/category): totals, bar chart and table from `/corporate/reports/summary`; trip details from `/corporate/reports/trips` with department/cost-center filters and CSV export. |
| `/business/app/settings` | Settings | Legal details (read-only), billing email/contact/national address (`PUT /corporate/account`), API keys when enabled (hidden on `404`). |

Any other path redirects to `/` (or `/business/app` inside the portal). Pages other than the landing and the driver screens are lazy-loaded route chunks, so Leaflet and the Markdown renderer only load where they are used.

## Language

Arabic (RTL) is the default. The AR/EN toggle in the header persists the choice in `localStorage` (`ata-language`), flips `<html lang dir>`, and is sent as `Accept-Language` on every request. All UI strings live in `src/i18n.ts`.

## Auth and API client

`src/lib/api.ts` wraps `fetch` with the base URL, `Accept-Language`, the bearer token of the relevant session, a single-flight refresh (per session) via `POST /auth/refresh` on `401`, and a typed `ApiError` mirroring the contract's `{ error: { code, message, details } }` envelope. File previews go through `GET /files/{id}` with the bearer token and are opened as object URLs; invoice PDFs and CSV exports are downloaded the same way (`downloadFile`).

There are two independent sessions: the driver portal (`localStorage` `ata-session`, `useAuth`, `RequireAuth`) and the corporate portal (`ata-business-session`, `useBusinessAuth`, `RequireCorporateAuth`, which also requires `user.roles` to contain `corporate_admin`). Signing in or out of one never touches the other; each `AuthProvider scope` only reacts to its own session events.

## Project layout

```
src/
  App.tsx              routes (lazy route chunks)
  main.tsx             providers (i18n, driver auth, business auth)
  i18n.ts              AR/EN dictionary + context (the EN map is typed against the AR keys, so parity is compile-checked)
  index.css            Tailwind v4 @theme tokens, shared classes, Markdown (.prose-ata) and Leaflet marker styles
  content/legal.ts     privacy policy + terms (AR/EN)
  components/          Icon, Button, Card, Keypad, OtpBoxes, OtpLogin, StatusBadge, Field, FileDrop, LanguageToggle,
                       Header, Footer, SiteLayout, Notice, States, Pagination, ProgressSteps, MapArt, StoreLinks,
                       Markdown, TrackingMap, LocationPicker, RequireAuth, RequireCorporateAuth,
                       business/ (ui primitives, StatusPills, BarChart)
  lib/                 api, config, types, session, auth, errors, format, catalog, corporate, help, geocode,
                       leaflet (default-icon fix for Vite), publicShare, useResource, useDebounced, useFlash
  pages/               Landing, DriverLogin, DriverPortal, portal/*, TripShare, help/*, legal/*,
                       business/ (BusinessLanding, BusinessLogin, BusinessJoin, BusinessLayout, app/*)
  providers/           I18nProvider, AuthProvider (scope: driver | business)
docker/                nginx.conf, 40-config.sh (runtime config.js)
Dockerfile             node:22-alpine build → nginx:1.27-alpine
```
