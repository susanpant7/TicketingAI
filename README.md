# Ticketing AI

IT Support AI application: ticketing plus a RAG-powered chat assistant.

| Folder    | Description                                             |
| --------- | ------------------------------------------------------- |
| `server/` | .NET 10 Web API (`TicketingAI.Api`, `TicketingAI.Data`) |
| `client/` | React + TypeScript (Vite) client                        |

## Run with Docker

```bash
docker compose up --build
```

- Client: http://localhost:5173 (nginx proxies `/api` to the server)
- API: http://localhost:5176/api/health
- SQLite data lives in the `server-data` volume.

## Run without Docker

```bash
dotnet run --project server/src/TicketingAI.Api   # http://localhost:5176
cd client && npm install && npm run dev           # http://localhost:5173 (proxies /api)
```
