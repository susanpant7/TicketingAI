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
- Ollama runs as its own container, so nothing else needs to be installed. The first start downloads `qwen2.5:7b` and `nomic-embed-text` (about 5 GB) into the `ollama-data` volume, and the server starts after that. Later starts reuse the models. Give Docker Desktop at least 10 GB of memory.
- If you change a model on the Settings page, pull it first: `docker compose exec ollama ollama pull <model>`.

## Run without Docker

```bash
dotnet run --project server/src/TicketingAI.Api   # http://localhost:5176
cd client && npm install && npm run dev           # http://localhost:5173 (proxies /api)
```
