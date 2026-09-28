# ATA Platform

| Folder | Project | Stack |
|---|---|---|
| `mobile_app/` | Mobile app | Flutter |
| `backend/` | Web APIs | ASP.NET Core (.NET 9) |
| `website/` | Public website | React + Vite + TypeScript |
| `dashboard/` | Admin dashboard | React + Vite + TypeScript |
| `res/` | Logos & design files | — |

## Run

```bash
# Flutter
cd mobile_app && flutter pub get && flutter run

# API
cd backend && dotnet run --project ATA.Api

# Website / Dashboard
cd website && npm install && npm run dev
cd dashboard && npm install && npm run dev
```
