# Ticketing AI

An internal support app for IT, HR and Finance. Employees chat with an AI assistant that answers from the company's knowledge (RAG). If an answer does not solve the problem, the assistant can create a ticket and assign it to a member of the right department.

| Part | What it is |
| --- | --- |
| `client/` | React + TypeScript (Vite, Tailwind) |
| `server/` | .NET 10 Web API (`TicketingAI.Api`) and the database layer (`TicketingAI.Data`), SQLite with the sqlite-vec extension |
| Ollama | Runs the models: `qwen2.5:7b` writes the answers, `nomic-embed-text` turns text into vectors |

More detail: the **Guide** page in the app (public; how to run, set up and use the app, the same as this README but for end users, source [how-to-run-and-use.md](client/src/features/docs/how-to-run-and-use.md)), the **Docs** page in the app (public, no login needed), with the developer docs on AI and RAG (ingestion, retrieval, the prompt, the model's reply, ticket creation, tuning and debugging; the source is [technical-guide.md](client/src/features/docs/technical-guide.md)), [chat workflow](server/src/TicketingAI.Api/Chat/README.md), [knowledge file format](server/src/TicketingAI.Api/Knowledge/README.md), [database commands](server/src/TicketingAI.Data/README.md).

## Run with Docker

Everything runs in containers, including Ollama. Nothing else needs to be installed.

**You need:** Docker Desktop, with at least 10 GB of memory (Settings, Resources) and about 10 GB of free disk space.

1. Create the secrets file. The server runs in `Production` mode, which has no built-in admin login or signing key.

   ```bash
   cp .env.example .env
   ```

   Edit `.env`: set `Admin__Password`, and set `Jwt__Key` to a random string of at least 32 characters (`openssl rand -base64 48`). `.env` is the one place for secrets, for Docker and for running without Docker, and it is not committed.

2. Start everything:

   ```bash
   docker compose up --build
   ```

   The first start downloads the two models (about 5 GB), and the app is not reachable until that finishes. Watch it with `docker compose logs -f pull-models`. Later starts take seconds.

3. Open http://localhost:5173 and log in with `Admin__Username` / `Admin__Password` from `.env`. Then follow [First-time setup in the app](#first-time-setup-in-the-app).

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

2. Create the secrets file, the same one Docker uses: `cp .env.example .env`, then edit it (admin password, `Jwt__Key`, and the chat API key if we use one). `dotnet run` reads `.env` by itself when the environment is `Development`, which is what `dotnet run` uses. Without it the server refuses to start.

3. Start the API (http://localhost:5176). The database is created and migrated automatically at startup, at `server/src/TicketingAI.Api/data/ticketing.db`.

   ```bash
   dotnet run --project server/src/TicketingAI.Api
   ```

   Use `dotnet watch run --project server/src/TicketingAI.Api --urls http://localhost:5176` to restart on code changes. A change to a controller route, an entity or a migration needs a full restart (Ctrl+R in the watch terminal).

4. Start the client (http://localhost:5173). The dev server proxies `/api` to the API.

   ```bash
   cd client
   npm install
   npm run dev
   ```

5. Open http://localhost:5173 and log in with `Admin__Username` / `Admin__Password` from `.env`. Then follow [First-time setup in the app](#first-time-setup-in-the-app).

**Configuration** (the AI assistant's settings are on the Settings page; `server/src/TicketingAI.Api/appsettings*.json` has the other settings that are not secret, and the `.env` file or environment variables the secrets, with `__` for a nested key, for example `Admin__Password`). Order of priority, last wins: `appsettings.json`, `appsettings.Development.json`, environment variables (the `.env` file is loaded into them):

| Setting | Default | Meaning |
| --- | --- | --- |
| `Admin:Username` / `Admin:Password` | none, from `.env` | The admin login. Required: the server refuses to start without it |
| `Jwt:Key` | none, from `.env` | Token signing key, at least 32 characters. Required |
| `ChatApi:ApiKey` | empty, from `.env` | The key of an online chat service (when the Chat API base URL has a path) |
| `ConnectionStrings:Default` | `Data Source=data/ticketing.db` | The SQLite file |

## Deploy the backend on a server (with free online models)

For the setup "client on Vercel, backend on a server, free chat and embedding APIs", the server needs only the .NET API and its SQLite
file. There is no Ollama and no GPU, so a small machine is enough (about 1 GB of memory).

1. **Get the code and the secrets on the server.** `git clone` the repository, then `cp .env.example .env` and fill in:
   - `Admin__Username`, `Admin__Password` and `Jwt__Key` (a long random string).
   - The key of the service, for example `GEMINI_API_KEY`.
   - Nothing else for the models: a new database starts with the first online service whose key is set, so with `GEMINI_API_KEY` it uses
     Gemini for the chat **and** for the embeddings (its model, key name and prefixes come with it). The Settings page can change it later.
   - `Cors__AllowedOrigins`, the address of the client, for example `https://ticketingai.susanpant.com.np`.
2. **Start it** (Docker needed): `docker compose -f docker-compose.prod.yml up -d --build`. The API listens on `127.0.0.1:5176` of the server only.
3. **Put https in front of it.** A page on https cannot call an http address. With the domain on Cloudflare, a **Cloudflare Tunnel** is
   the easiest: it needs no open port and gives a free https address. In the tunnel, add a public hostname such as
   `api.susanpant.com.np` that points to `http://localhost:5176`. A reverse proxy (Caddy, nginx) on the server works as well.
4. **Point the client at it.** In Vercel, set `VITE_API_URL` to that https address and redeploy (see the next section).
5. **Set it up in the app:** log in as the admin, then load the departments and users and upload the knowledge files. The embeddings are
   made by the online service, so no Ollama is needed.

Things to know about the server:
- **The database must survive restarts.** The SQLite file is in the `server-data` volume. Hosts with a disk that is wiped on every deploy
  (the free plan of Render, and similar) would lose the users, tickets and knowledge. A small virtual machine (any provider, x64) keeps it.
  Back the volume up from time to time.
- **Use an x64 machine.** The vector search library (sqlite-vec) has not been checked on ARM.
- **Free plans have limits.** The chat and the embeddings share the limits of the key, and uploading the knowledge makes several
  embedding requests. If the service answers "too many requests", wait a moment and try again.
- **Only the client's address may call the API** (`Cors__AllowedOrigins`). Everything else is the same as locally.

### On Render (a Docker web service)

Render builds the same `server/Dockerfile` from the GitHub repository. In the Render dashboard: **New**, **Web Service**, and connect the repository.

| Render setting | Value |
| --- | --- |
| Language | Docker |
| Root Directory | `server` |
| Dockerfile Path | `./Dockerfile` |
| Health Check Path | `/api/health` |
| Environment variable `PORT` | `8080` (the port the API listens on in the image, so Render knows where to send the traffic) |
| Environment variables | the same as the `.env` of step 1: `Admin__Username`, `Admin__Password`, `Jwt__Key`, `GEMINI_API_KEY`, `Cors__AllowedOrigins` (Render can import a whole `.env` file with **Add from .env**) |
| Disk (paid plans only) | mount path `/app/data`, 1 GB: this keeps the SQLite database |

Render gives the service an https address such as `https://ticketing-ai-api.onrender.com`, so no tunnel or proxy is needed. Use that address as `VITE_API_URL` in Vercel (no slash at the end), and put the Vercel address in `Cors__AllowedOrigins`. Every push to the branch deploys again.

Before you choose a plan:
- **The free plan keeps no data.** Its disk is wiped on every deploy and whenever the service wakes up after sleeping (about 15 minutes without a request), so the users, tickets and knowledge are lost. It also takes about a minute to wake up. It is fine for a demo (**Settings**, **Manage App Data** loads the defaults again), not for real use.
- **To keep the data, use a paid instance with a disk** mounted at `/app/data`, as in the table. If the service then stops with "unable to open database file", the disk is not writable for the container's non-root user: check the disk's mount path first, because the image runs as a non-root user.
- **Memory:** the free 512 MB is enough for the API alone, since the models are online.

## Host the client on Vercel

The client is a static site, so it can be hosted apart from the API. In Vercel, import the repository and set:

| Vercel setting | Value |
| --- | --- |
| Root Directory | `client` |
| Framework Preset | Vite (build command `npm run build`, output `dist`) |
| Environment variable `VITE_API_URL` | The public **https** address of the API, without a trailing slash, for example `https://api.example.com` |

`VITE_API_URL` is read when the client is built, so after changing it, redeploy. On the API side, set `Cors__AllowedOrigins` to the Vercel address (for example `https://ticketingai.susanpant.com.np`, several separated by commas), otherwise the browser blocks the calls. The API must be reachable over https, because a page on https cannot call an http address. `client/vercel.json` makes page addresses such as `/guide` or `/tickets` open the app when typed in directly or reloaded; without it Vercel answers 404.

## Use an external API for the chat (Gemini, Cloudflare)

The chat answers can come from a model on our own server (Ollama, the default) or from an external service that speaks the OpenAI chat format. The app supports both, and an admin switches between them on the **Settings** page, with no code change. Embeddings always stay on Ollama, so the uploaded knowledge stays valid when the chat provider changes.

**0. Where the settings live.** Everything about the AI assistant that is not secret is a setting in the database, edited on the **Settings** page (AI assistant tab): where Ollama runs, the chat API base URL, the models, the prompt and how the knowledge is searched. Only the key stays outside, in `.env`. Nothing writes the settings when the server starts: a setting with no row uses its default, and **Restore defaults** (Settings, AI assistant tab) saves them all. The defaults pick the first online service whose key is set in `.env` (Gemini with `GEMINI_API_KEY`) for the chat and the embeddings, and otherwise Ollama. In Docker the Ollama address is the Ollama container. To start with other values, set `AiDefaults__EmbeddingBaseUrl`, `AiDefaults__ChatBaseUrl` or `AiDefaults__ChatModel` in `.env`. Restore defaults resets every setting, so on an existing database we change the Settings page instead (step 2).

**1. Put the key in `.env`.** Every service has its own key variable, which `.env.example` lists (`GEMINI_API_KEY`, `GROQ_API_KEY`, `OPENROUTER_API_KEY`, `MISTRAL_API_KEY`, `CEREBRAS_API_KEY`, `CLOUDFLARE_API_KEY`, `OPENAI_API_KEY`). We add the line `GROQ_API_KEY=<the key>` (for Groq, and so on) to the `.env` file, the same file for Docker and for running without Docker. The Settings page shows the **API key name** of each model and whether that variable is set, and saves only the name, never the key. A key is a secret, so it never goes in `appsettings.json` (that file is committed to git) and is never stored in the database. After a change, we restart the API, or run `docker compose up -d` for Docker.

**2. Choose the model** on the Settings page, AI assistant tab: click **Use** on the service in the "Services we can use" list (or on a model we saved), which sets the address, the model and the key name together. **Edit** on any model (a saved one, a service, or the card "In use now") changes them; editing a service saves a copy as our own model. Which models a key can use differs from key to key, so click **View models** on the service's row: a dialog lists the chat models the service reports, each with a **Test** button (one tiny request, nothing saved; a failure is shown in a second dialog above the list) and a **Select** button. A service can list models a key cannot use (Gemini does), which is why a model is tested before it is selected. At the top of the chat model box, **Next model** and **Start from the beginning** go through the chat models of every service we have a key for, one after another: each is tested, the dialog stops at the first one that answers and shows its response time, **Select** uses it and **Next** tries the next one. A model that gives an error or no answer is skipped by itself, and so is the rest of a service whose limit was reached. There is no separate provider setting: an address with no path (`http://localhost:11434`) is Ollama, and an address with a path is an online service.

| Setting | Gemini | Cloudflare Workers AI |
| --- | --- | --- |
| Chat API base URL | `https://generativelanguage.googleapis.com/v1beta/openai` | `https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1` |
| Chat model | the model name from Google AI Studio, for example `gemini-3.8-flash` | for example `@cf/meta/llama-3.1-8b-instruct-fp8` |

The key comes from Google AI Studio (Gemini), or from a Cloudflare API token with Workers AI access (Cloudflare). Check each service's current model names and free limits. To go back to the local model, click **Use** on "Ollama (on our own machine)", or **Edit** the model in use and set the address to `http://localhost:11434` (`http://ollama:11434` in Docker) and the model to `qwen2.5:7b`.

**Things to know.** The prompt was tuned on `qwen2.5:7b`, so re-test the ticket flow with another model. The questions and the knowledge sections are sent to the external service, so don't use real company data on a free tier without reading its terms. When only the API is used, the chat model does not have to be downloaded: remove it from the `pull-models` line in `docker-compose.yml` (keep `nomic-embed-text`).

## First-time setup in the app

A new database is empty. Log in as the admin and do these in order:

1. **Departments:** click "Load default departments" (IT, HR, Finance), or add your own.
2. **Users:** click "Load default users", enter one password for all of them, and they are divided over the departments. You can also add users one by one. Every user belongs to a department.
3. **Knowledge:** choose a department and upload its knowledge files. The files must start with `title:` and `department:` lines, then `## Section title` sections, and the department in the file must match the selected one. Sample files are in `server/src/TicketingAI.Api/Knowledge/Documents`. "Add" keeps the existing knowledge, "Flush and Add" deletes it first.
4. **Settings:** click **Restore defaults** on the AI assistant tab once. Nothing is saved at startup, so until then the app uses the built-in defaults; this saves them as settings and picks the chat and embedding model from the keys in `.env` (Gemini with `GEMINI_API_KEY`, otherwise Ollama). It resets every setting, so we do it once, on a new database.

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

**Changing the embedding model** (`nomic-embed-text`) works the same way, in the Embedding group of the Settings page (**Use** on a saved model or a service, with **View models** to see what a key can use). The difference: vectors from different models cannot be compared, so **Use converts all the stored knowledge to the new model**: every section is embedded again from its text. It can take a minute, and the change is saved only when it worked, so a wrong key or a missing model changes nothing. The model's two prefixes (`search_document:` and `search_query:` for `nomic-embed-text`, none for most online ones) are set together with it.

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
| `docker compose up` fails, or the server stops at startup and says that `Admin:Password` or `Jwt:Key` is required | The `.env` file is missing or incomplete. Copy `.env.example` to `.env` and fill it in |
| Chat or upload fails with "model not found" | The model on the Settings page is not in Ollama. Pull it (`ollama pull <model>`, or inside Docker `docker compose exec ollama ollama pull <model>`) |
| "Cannot reach the server" | The API is not running, or in Docker it is still starting or crashed. Check `docker compose logs server` |
| Replies take 15 to 30 seconds | Normal on a CPU without a GPU. A 7B model reads the knowledge sections before it writes the first word |
| Docker reply is very slow or the model is killed | Docker Desktop has too little memory. Raise it to at least 10 GB |
| Chat has no knowledge to answer from | Nothing has been uploaded yet. Upload the files |
