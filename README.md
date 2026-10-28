# Ticketing AI

IT Support AI application: ticketing plus a RAG-powered chat assistant.

| Folder      | Description                                        |
| ----------- | -------------------------------------------------- |
| `server/`  | .NET 10 Web API (`TicketingAI.Api`, `TicketingAI.Data`) |
| `client/` | React + TypeScript (Vite) client                   |

## Run

```bash
# API  -> http://localhost:5176
dotnet run --project server/src/TicketingAI.Api

# Client -> http://localhost:5173
cd client && cp .env.example .env && npm install && npm run dev
```
