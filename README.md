# Ticketing AI

An internal support app for IT, HR and Finance. Employees chat with an AI assistant that answers from the company's knowledge (RAG). If an answer does not solve the problem, the assistant can create a ticket and assign it to a member of the right department.

| Part | What it is |
| --- | --- |
| `client/` | React + TypeScript (Vite, Tailwind) |
| `server/` | .NET 10 Web API (`TicketingAI.Api`) and the database layer (`TicketingAI.Data`), SQLite with the sqlite-vec extension |
| Ollama | Runs the models: `qwen2.5:7b` writes the answers, `nomic-embed-text` turns text into vectors |

More detail: the **Guide** page in the app (public; how to run, set up and use the app, the same as this README but for end users, source [how-to-run-and-use.md](client/src/features/docs/how-to-run-and-use.md)), the **Docs** page in the app (public, no login needed), with the developer docs on AI and RAG (ingestion, retrieval, the prompt, the model's reply, ticket creation, tuning and debugging; the source is [technical-guide.md](client/src/features/docs/technical-guide.md)), [chat workflow](server/src/TicketingAI.Api/Chat/README.md), [knowledge file format](server/src/TicketingAI.Api/Knowledge/README.md), [database commands](server/src/TicketingAI.Data/README.md).

## Run locally

**You need:** the [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js 24 with npm, and [Ollama](https://ollama.com/download), which runs the models on our own computer (the default). An online service's key is optional.

1. **Start Ollama and download the models** (about 5 GB, once). Ollama listens on http://localhost:11434. This is the default for the chat and the embeddings.

   ```bash
   ollama serve                  # skip this if the Ollama desktop app is already running
   ollama pull qwen2.5:7b
   ollama pull nomic-embed-text
   ```

2. Create the secrets file, `cp .env.example .env`, then edit it (admin password, `Jwt__Key`, and the key of an online service if we use one). `dotnet run` reads `.env` by itself when the environment is `Development`, which is what `dotnet run` uses. Without it the server refuses to start.

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
| `ConnectionStrings:Default` | `Data Source=data/ticketing.db` | The SQLite file |

## Run the backend in Docker

The repository has one Dockerfile, `server/Dockerfile`, for the API only. It is used to deploy the backend on a server (for example Render). The client is not in Docker: locally it runs with `npm run dev`, and online it is hosted on Vercel.

```bash
docker build -f server/Dockerfile -t ticketing-ai-api .
docker run -p 5176:8080 --env-file .env -v ticketing-ai-data:/app/data ticketing-ai-api
```

The build context is the repository root. The health check is http://localhost:5176/api/health. The database is in the `ticketing-ai-data` volume. Inside the container, a model on our own computer is reached at `http://host.docker.internal:11434`.

## Deploy the backend on a server (with free online models)

For the setup "client on Vercel, backend on a server, free chat and embedding APIs", the server needs only the .NET API and its SQLite
file. There is no Ollama and no GPU, so a small machine is enough (about 1 GB of memory).

1. **Get the code and the secrets on the server.** `git clone` the repository, then `cp .env.example .env` and fill in:
   - `Admin__Username`, `Admin__Password` and `Jwt__Key` (a long random string).
   - The key of the service, for example `GEMINI_API_KEY`.
   - For the models: a new database has no chat or embedding model selected. After the first login we choose the online service
     on the Settings page (**Search model from the beginning** or **View models**, then **Select**), for the chat and for the embeddings. Or we
     set `AiDefaults__ChatBaseUrl` and `AiDefaults__EmbeddingBaseUrl` (for example Gemini's addresses) in `.env`, so a new database starts with them.
   - `Cors__AllowedOrigins`, the address of the client, for example `https://ticketingai.susanpant.com.np`.
2. **Start it.** Either `dotnet run --project server/src/TicketingAI.Api` (the .NET 10 SDK is needed), or with the single Dockerfile, [below](#run-the-backend-in-docker). The API listens on port 5176 (Docker: 8080 inside the container).
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
| Root Directory | leave **empty** |
| Dockerfile Path | `./server/Dockerfile` |
| Docker Build Context Directory | leave the default (`.`, the repository root) |
| Health Check Path | `/api/health` |
| Environment variable `PORT` | `8080` (the port the API listens on in the image, so Render knows where to send the traffic) |
| Environment variables | the same as the `.env` of step 1: `Admin__Username`, `Admin__Password`, `Jwt__Key`, `GEMINI_API_KEY`, `Cors__AllowedOrigins` (Render can import a whole `.env` file with **Add from .env**) |
| Disk (paid plans only) | mount path `/app/data`, 1 GB: this keeps the SQLite database |

The image is built from the repository root (the Dockerfile copies `server/src/...`), so the build context must stay the repository root. If the build says `"/src": not found`, the context was set to another folder: set it back to `.`. Under **Advanced**, **Build Filters** with the included path `server/**` makes a push that only changes the client skip the API's deploy.

Render gives the service an https address such as `https://ticketing-ai-api.onrender.com`, so no tunnel or proxy is needed. Use that address as `VITE_API_URL` in Vercel (no slash at the end), and put the Vercel address in `Cors__AllowedOrigins`. Every push to the branch deploys again.

Before you choose a plan:
- **The free plan keeps no data.** Its disk is wiped on every deploy and whenever the service wakes up after sleeping (about 15 minutes without a request), so the users, tickets and knowledge are lost. It also takes about a minute to wake up. It is fine for a demo (**Settings**, **Manage App Data** loads the defaults again), not for real use.
- **To keep the data, use a paid instance with a disk** mounted at `/app/data`, as in the table. If the service then stops with "unable to open database file", the disk is not writable for the container's non-root user: check the disk's mount path first, because the image runs as a non-root user.
- **Memory:** the free 512 MB is enough for the API alone, since the models are online.

## Use the Ollama on our computer from the server

A server such as Render cannot reach `localhost` on our computer. A Cloudflare Tunnel gives the Ollama on our computer a public https address:

```bash
cloudflared tunnel login
cloudflared tunnel create ollama
cloudflared tunnel route dns ollama ollama-k3x9f.susanpant.com.np   # use a long random name, there is no login on this address
cloudflared tunnel run --url http://localhost:11434 --http-host-header localhost:11434 ollama
```

In the app, Settings, AI assistant, Embeddings, we **Edit** the entry "Ollama on our computer, from a server" in the list (it comes with the address `https://<my-ollama>.susanpant.com.np`), put our own address in it, save it, then **View models** and **Select** `nomic-embed-text` on the saved model. The computer, Ollama and the tunnel must stay on, because every question is embedded there.

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

**0. Where the settings live.** Everything about the AI assistant that is not secret is a setting in the database, edited on the **Settings** page (AI assistant tab): where Ollama runs, the chat API base URL, the models, the prompt and how the knowledge is searched. Only the key stays outside, in `.env`. Nothing writes the settings when the server starts: a setting with no row uses its default, and **Load default AI assistant data** (Settings, Manage App Data tab) saves them all. The defaults select no chat or embedding model: an admin chooses them on the same tab (for Ollama on this computer, `http://localhost:11434`). Choosing an embedding model cleans the stored knowledge and inserts it again with that model, so the knowledge files are loaded after that. To start with a model already chosen, set `AiDefaults__EmbeddingBaseUrl`, `AiDefaults__ChatBaseUrl` or `AiDefaults__ChatModel` in `.env`. Loading the defaults resets every setting, so on an existing database we change the Settings page instead (step 2).

**1. Put the key in `.env`.** Every service has its own key variable, which `.env.example` lists (`GEMINI_API_KEY`, `GROQ_API_KEY`, `OPENROUTER_API_KEY`, `MISTRAL_API_KEY`, `CEREBRAS_API_KEY`, `CLOUDFLARE_API_KEY`, `OPENAI_API_KEY`, `JINA_API_KEY`, `COHERE_API_KEY`). We add the line `GROQ_API_KEY=<the key>` (for Groq, and so on) to the `.env` file, the same file for Docker and for running without Docker. The Settings page shows the **API key name** of each model and whether that variable is set, and saves only the name, never the key. A key is a secret, so it never goes in `appsettings.json` (that file is committed to git) and is never stored in the database. After a change, we restart the API, or run `docker compose up -d` for Docker.

**2. Choose the model** on the Settings page, AI assistant tab: click **View models** on the service in the "Services we can use" list (or on a model we saved), then **Select** a model, which sets the address, the model and the key name together. **Edit** on any model (a saved one, a service, or the card "In use now") changes them; editing a service saves a copy as our own model. Which models a key can use differs from key to key, so click **View models** on the service's row: a dialog lists the chat models the service reports, each with a **Test** button (one tiny request, nothing saved; a failure is shown in a second dialog above the list) and a **Select** button. A service can list models a key cannot use (Gemini does), which is why a model is tested before it is selected. At the top of the chat model box and of the embedding model box, **Next model** and **Search model from the beginning** go through the models of every service we have a key for, one after another: each is tested, the dialog stops at the first one that answers and shows its response time, **Select** uses it and **Next** tries the next one. A model that gives an error or no answer is skipped by itself, and so is the rest of a service whose limit was reached. There is no separate provider setting: an address with no path (`http://localhost:11434`) is Ollama, and an address with a path is an online service.

| Setting | Gemini | Cloudflare Workers AI |
| --- | --- | --- |
| Chat API base URL | `https://generativelanguage.googleapis.com/v1beta/openai` | `https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1` |
| Chat model | the model name from Google AI Studio, for example `gemini-3.8-flash` | for example `@cf/meta/llama-3.1-8b-instruct-fp8` |

The key comes from Google AI Studio (Gemini), or from a Cloudflare API token with Workers AI access (Cloudflare). Check each service's current model names and free limits. To go back to a model on our own computer, click **View models** on "Ollama (on our own machine)" and **Select** the model, or **Edit** the model in use and set the address to `http://localhost:11434` (`http://host.docker.internal:11434` from a Docker container) and the model to `qwen2.5:7b`.

**Things to know.** The prompt was tuned on `qwen2.5:7b`, so re-test the ticket flow with another model. The questions and the knowledge sections are sent to the external service, so don't use real company data on a free tier without reading its terms.

## First-time setup in the app

A new database is empty. Log in as the admin and do these in order:

1. **Departments:** click "Load default departments" (IT, HR, Finance), or add your own.
2. **Users:** click "Load default users", enter one password for all of them, and they are divided over the departments. You can also add users one by one. Every user belongs to a department.
3. **Knowledge:** choose a department and upload its knowledge files. The files must start with `title:` and `department:` lines, then `## Section title` sections, and the department in the file must match the selected one. Sample files are in `server/src/TicketingAI.Api/Knowledge/Documents`. "Add" keeps the existing knowledge, "Flush and Add" deletes it first.
4. **Settings:** click **Load default AI assistant data** on the Manage App Data tab once. Nothing is saved at startup, so until then the app uses the built-in defaults; this saves them as settings, without a chat or embedding model. The models are chosen on the same tab, and the knowledge files are uploaded after an embedding model is selected (it cleans and reinserts the stored knowledge). **Load all default data** (Settings, Manage App Data) loads departments, users and settings the same way, without the knowledge files. It resets every setting, so we do it once, on a new database.

Then log in as an employee (for example `aarav` with the password you chose), open the chat button at the bottom right, and ask a question. If the answer does not help, say so, and the assistant offers to create a ticket. Tickets are listed on the Tickets page, where the assignee can change the status.

## Changing the AI model

**The default models are `qwen2.5:7b` (chat answers) and `nomic-embed-text` (embeddings).** The defaults live in `server/src/TicketingAI.Data/Persistence/SettingDefaults.cs` and are stored in the database as settings. `qwen2.5:7b` needs about 8 GB of memory or more. A smaller model such as `qwen2.5:3b` is faster and needs about half the memory, but its answers should be re-tested, because the prompt was tuned on the 7B model.

To switch the model, open the Settings page, AI assistant tab, and use **View models** (then **Select**), **Edit** or **Next model** (see above). For an online service nothing has to be downloaded: the key in `.env` is enough.

For a model on our own computer (Ollama), the model must exist there first: run `ollama pull <model>` on that computer (for example `ollama pull qwen2.5:3b`), then **Select** it on the Settings page. It applies to the next message, with no restart.

**Changing the embedding model** (`nomic-embed-text`) works the same way, in the Embedding group of the Settings page (**View models** on a saved model or a service, then **Select** to see what a key can use). The difference: vectors from different models cannot be compared, so **Use converts all the stored knowledge to the new model**: every section is embedded again from its text. It can take a minute, and the change is saved only when it worked, so a wrong key or a missing model changes nothing. The two prefixes (`search_document:` and `search_query:`) are the same for every embedding model and are not settings.

**Where to get embeddings for free.** The embedding box lists Ollama (on our own machine), Google Gemini, Mistral, Jina AI, Cohere, OpenAI (paid, but very cheap) and Cloudflare Workers AI. Groq and Cerebras have no embedding models (their model lists show only chat, speech and safety models), so with Groq for the chat, use another service for the embeddings. Each service has its own key variable in `.env`, and the free plans have limits.

## Start with an empty database

The database is one SQLite file. To start from zero (this deletes the settings, the tickets and the knowledge), stop the API and delete the file (by default `server/src/TicketingAI.Api/data/ticketing.db*`, see `ConnectionStrings__Default`). It is created again at the next start. In the Docker container, remove the volume: `docker volume rm ticketing-ai-data`.

After a fresh database, do the [first-time setup](#first-time-setup-in-the-app) again: load departments and users, and upload the knowledge files.

## Troubleshooting

| Problem | Cause and fix |
| --- | --- |
| Login says "something went wrong", the server stops with `table ... already exists` | An old database does not match the migrations. [Start with an empty database](#start-with-an-empty-database) |
| The server stops at startup and says that `Admin:Password` or `Jwt:Key` is required | The `.env` file is missing or incomplete. Copy `.env.example` to `.env` and fill it in |
| Chat or upload fails with "model not found" | The model on the Settings page is not offered by the service (or, for Ollama, not pulled). Use **View models** to see what a key can use, or for Ollama run `ollama pull <model>` on that computer |
| "Cannot reach the server" | The API is not running, or it crashed. Look at its terminal (or `docker logs <container>`) |
| Replies take 15 to 30 seconds | Normal on a CPU without a GPU. A 7B model reads the knowledge sections before it writes the first word |
| Chat has no knowledge to answer from | Nothing has been uploaded yet. Upload the files |
