# Ticketing AI

An internal support app for IT, HR and Finance. Employees chat with an AI assistant that answers from the company's knowledge (RAG). If an answer does not solve the problem, the assistant can create a ticket and assign it to a member of the right department.

| Part | What it is |
| --- | --- |
| `client/` | React + TypeScript (Vite, Tailwind) |
| `server/` | .NET 10 Web API (`TicketingAI.Api`) and the database layer (`TicketingAI.Data`), SQLite with the sqlite-vec extension |
| Ollama | Runs the models: `qwen2.5:7b` writes the answers, `nomic-embed-text` turns text into vectors |

More detail: the **Docs** page in the app (public, no login needed), with the developer docs on AI and RAG (ingestion, retrieval, the prompt, the model's reply, ticket creation, tuning and debugging; the source is [technical-guide.md](client/src/features/docs/technical-guide.md)), [chat workflow](server/src/TicketingAI.Api/Chat/README.md), [knowledge file format](server/src/TicketingAI.Api/Knowledge/README.md), [database commands](server/src/TicketingAI.Data/README.md).

## Run with Docker

Everything runs in containers, including Ollama. Nothing else needs to be installed.

**You need:** Docker Desktop, with at least 10 GB of memory (Settings, Resources) and about 10 GB of free disk space.

1. Create the secrets file. The server runs in `Production` mode, which has no built-in admin login or signing key.

   ```bash
   cp .env.example .env
   ```

   Edit `.env`: set `ADMIN_PASSWORD`, and set `JWT_KEY` to a random string of at least 32 characters (`openssl rand -base64 48`).

2. Start everything:

   ```bash
   docker compose up --build
   ```

   The first start downloads the two models (about 5 GB), and the app is not reachable until that finishes. Watch it with `docker compose logs -f pull-models`. Later starts take seconds.

3. Open http://localhost:5173 and log in with `ADMIN_USERNAME` / `ADMIN_PASSWORD` from `.env`. Then follow [First-time setup in the app](#first-time-setup-in-the-app).

Only the client port (5173) is published. nginx proxies `/api` to the server, so the health check is http://localhost:5173/api/health.

| Command | What it does |
| --- | --- |
| `docker compose up --build -d` | Start in the background (rebuilds after code changes) |
| `docker compose logs -f server` | Follow the API logs (errors in production show here, not in the browser) |
| `docker compose down` | Stop and remove the containers. Data and models are kept |
| `docker compose down -v` | Also delete the volumes: database, settings, tickets and the downloaded models |
| `docker volume rm ticketing-ai_server-data` | Delete only the database, keeping the models (run `docker compose down` first) |

Data lives in two volumes: `server-data` (the SQLite database) and `ollama-data` (the models).

**Changing a model on the Settings page:** the model must exist in the Ollama container first: `docker compose exec ollama ollama pull <model>`. To download it automatically on a new machine, also add it to the `pull-models` service in `docker-compose.yml`.

## Run without Docker

**You need:** the [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 24 with npm, and [Ollama](https://ollama.com/download).

1. Start Ollama and download the models (about 5 GB, once). Ollama listens on http://localhost:11434.

   ```bash
   ollama serve                  # skip this if the Ollama desktop app is already running
   ollama pull qwen2.5:7b
   ollama pull nomic-embed-text
   ```

2. Start the API (http://localhost:5176). The database is created and migrated automatically at startup, at `server/src/TicketingAI.Api/data/ticketing.db`.

   ```bash
   dotnet run --project server/src/TicketingAI.Api
   ```

   Use `dotnet watch run --project server/src/TicketingAI.Api --urls http://localhost:5176` to restart on code changes. A change to a controller route, an entity or a migration needs a full restart (Ctrl+R in the watch terminal).

3. Start the client (http://localhost:5173). The dev server proxies `/api` to the API.

   ```bash
   cd client
   npm install
   npm run dev
   ```

4. Open http://localhost:5173 and log in as `admin` / `admin` (the development login from `appsettings.Development.json`). Then follow [First-time setup in the app](#first-time-setup-in-the-app).

**Configuration** (`server/src/TicketingAI.Api/appsettings*.json`, or environment variables with `__` for a nested key, for example `Ollama__BaseUrl`):

| Setting | Default | Meaning |
| --- | --- | --- |
| `Admin:Username` / `Admin:Password` | `admin` / `admin` (development only) | The admin login. Required in production |
| `Jwt:Key` | a development key | Token signing key, at least 32 characters. Set your own in production |
| `Ollama:BaseUrl` | `http://localhost:11434` | Where Ollama is |
| `ConnectionStrings:Default` | `Data Source=data/ticketing.db` | The SQLite file |

## First-time setup in the app

A new database is empty. Log in as the admin and do these in order:

1. **Departments:** click "Load default departments" (IT, HR, Finance), or add your own.
2. **Users:** click "Load default users", enter one password for all of them, and they are divided over the departments. You can also add users one by one. Every user belongs to a department.
3. **Knowledge:** choose a department and upload its knowledge files. The files must start with `title:` and `department:` lines, then `## Section title` sections, and the department in the file must match the selected one. Sample files are in `server/src/TicketingAI.Api/Knowledge/Documents`. "Add" keeps the existing knowledge, "Flush and Add" deletes it first.
4. **Settings:** optional. The defaults work. "Restore defaults" puts them back.

Then log in as an employee (for example `aarav` with the password you chose), open the chat button at the bottom right, and ask a question. If the answer does not help, say so, and the assistant offers to create a ticket. Tickets are listed on the Tickets page, where the assignee can change the status.

## Changing the AI model

**The default models are `qwen2.5:7b` (chat answers) and `nomic-embed-text` (embeddings).** The defaults live in `server/src/TicketingAI.Data/Persistence/SettingDefaults.cs` and are stored in the database as settings. `qwen2.5:7b` needs about 8 GB of memory or more. A smaller model such as `qwen2.5:3b` is faster and needs about half the memory, but its answers should be re-tested, because the prompt was tuned on the 7B model.

Example: switch the chat model from `qwen2.5:7b` to `qwen2.5:3b`. Two things must change, because the model has to exist in Ollama **and** the app has to be told to use it.

**With Docker**

1. **Docker side: download the model.** In `docker-compose.yml`, in the `pull-models` service, change the `entrypoint` line:

   ```yaml
   # before
   entrypoint: ["/bin/sh", "-c", "ollama pull qwen2.5:7b && ollama pull nomic-embed-text"]
   # after
   entrypoint: ["/bin/sh", "-c", "ollama pull qwen2.5:3b && ollama pull nomic-embed-text"]
   ```

   Then run `docker compose up -d`. The changed service runs again and downloads the model. To skip the compose edit for a one-off test, run `docker compose exec ollama ollama pull qwen2.5:3b` instead (the compose edit is only needed so a new machine downloads it automatically).

2. **App side: use the model.** Log in as admin, open **Settings**, find **Chat model**, click **Edit**, change the value to `qwen2.5:3b`, and save. It applies to the next message, with no restart.

3. **Optional: make it the default for new installs.** On the Settings page, **Restore defaults** puts the model back to whatever the default in `SettingDefaults.cs` says, which is `qwen2.5:7b`. To make `qwen2.5:3b` the default, change the `ChatModel` value in `SettingDefaults.cs` (`"qwen2.5:7b"` to `"qwen2.5:3b"`), rebuild (`docker compose up --build`), and click **Restore defaults** once on an existing database.

**Without Docker:** run `ollama pull qwen2.5:3b`, then do step 2.

**Changing the embedding model** (`nomic-embed-text`) is different: it **deletes all the knowledge** (old and new vectors cannot be compared) and the two prefixes (`search_document:` and `search_query:`) must match the new model. Do the same pull, change **Embedding model** and the prefixes on the Settings page, then upload the knowledge files again.

## Clean restart with Docker

Use these when something is stuck or you want a fresh start. Run them in the project folder.

**Fresh database, keep the downloaded models** (for example after changing the migrations, or an error such as `table ... already exists`):

```bash
docker compose down
docker volume rm ticketing-ai_server-data
docker compose up --build
```

**Clean everything and start over** (deletes the database, the settings, the tickets **and the downloaded models**, so the 5 GB download happens again):

```bash
docker compose down -v --remove-orphans
docker compose build --no-cache
docker compose up
```

After a fresh database, do the [first-time setup](#first-time-setup-in-the-app) again: load departments and users, and upload the knowledge files.

## Troubleshooting

| Problem | Cause and fix |
| --- | --- |
| Docker: login says "something went wrong", the server keeps restarting with `table ... already exists` | An old database in the `server-data` volume does not match the migrations. `docker compose down`, `docker volume rm ticketing-ai_server-data`, `docker compose up --build` |
| Docker: `docker compose up` stops with "Set ADMIN_PASSWORD" | The `.env` file is missing or empty. Copy `.env.example` to `.env` |
| Chat or upload fails with "model not found" | The model on the Settings page is not in Ollama. Pull it (`ollama pull <model>`, or inside Docker `docker compose exec ollama ollama pull <model>`) |
| "Cannot reach the server" | The API is not running, or in Docker it is still starting or crashed. Check `docker compose logs server` |
| Replies take 15 to 30 seconds | Normal on a CPU without a GPU. A 7B model reads the knowledge sections before it writes the first word |
| Docker reply is very slow or the model is killed | Docker Desktop has too little memory. Raise it to at least 10 GB |
| Chat has no knowledge to answer from | Nothing has been uploaded yet, or the embedding model was changed (that deletes the knowledge). Upload the files again |
