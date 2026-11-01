# How to run and use the app

Ticketing AI is a support app for a company. Employees chat with an AI assistant that answers from the company's own knowledge. If the answer does not solve the problem, the assistant can create a ticket and assign it to someone in the right department. This page explains how to run the app, how to set it up the first time, and how to use it. How the AI works is on the [Docs](/docs) page.

**The code is on GitHub:** [github.com/susanpant7/TicketingAI](https://github.com/susanpant7/TicketingAI). To get it:

```bash
git clone https://github.com/susanpant7/TicketingAI.git
cd TicketingAI
```

All the commands below are run from this project folder.

## 1. Run the app with Docker

This is the easiest way. Everything runs in containers, including the AI models, so nothing else needs to be installed.

**What we need:** Docker Desktop, with at least 10 GB of memory (Settings, Resources) and about 10 GB of free disk space.

**Step 1. Create the secrets file.** In the project folder we copy the example file and edit it. The server runs in production mode, which has no built-in admin login or signing key.

```bash
cp .env.example .env
```

In `.env` we set `Admin__Password`, and we set `Jwt__Key` to a random string of at least 32 characters (`openssl rand -base64 48` makes one).

**Step 2. Start everything.**

```bash
docker compose up --build
```

The first start downloads the two AI models (about 5 GB), and the app is not reachable until that finishes. `docker compose logs -f pull-models` shows the progress. Later starts take seconds.

**Step 3. Open the app.** We go to http://localhost:5173 and log in with `Admin__Username` and `Admin__Password` from `.env`. Then we follow section 3 below.

Useful commands:

| Command | What it does |
| --- | --- |
| `docker compose up --build -d` | Starts the app in the background. `--build` rebuilds after code changes |
| `docker compose logs -f server` | Follows the API logs. In production an error shows here, not in the browser |
| `docker compose down` | Stops the app. The data and the models are kept |
| `docker compose down -v` | Stops the app and also deletes the database, the settings, the tickets **and the downloaded models** |
| `docker volume rm ticketing-ai_server-data` | Deletes only the database, keeping the models (run `docker compose down` first) |

The data lives in two Docker volumes: `server-data` (the database) and `ollama-data` (the models).

## 2. Run the app without Docker

**What we need:** the .NET 10 SDK, Node.js 24 with npm, and [Ollama](https://ollama.com/download), the program that runs the AI models.

**Step 1. Start Ollama and download the models** (about 5 GB, only once).

```bash
ollama serve                  # not needed if the Ollama desktop app is already running
ollama pull qwen2.5:7b        # writes the answers
ollama pull nomic-embed-text  # turns text into numbers
```

**Step 2. Create the secrets file.** It is the same `.env` file that Docker uses. We copy the example and edit it (the admin password, `Jwt__Key`, and the chat API key if we use one):

```bash
cp .env.example .env
```

`dotnet run` reads `.env` by itself in development. Without it the server refuses to start.

**Step 3. Start the API** (http://localhost:5176). The database is created and updated by itself at startup.

```bash
dotnet run --project server/src/TicketingAI.Api
```

**Step 4. Start the client** (http://localhost:5173).

```bash
cd client
npm install
npm run dev
```

**Step 5. Open the app.** We go to http://localhost:5173 and log in with `Admin__Username` and `Admin__Password` from `.env`. Then we follow section 3 below.

## 3. Set up the app for the first time

A new database is empty. We log in as the admin and do these four things, in this order.

1. **Departments.** Open **Departments** and click **Load default departments** (IT, HR and Finance), or add our own with **Add department**. Each department has a description of what it handles and a contact. The assistant uses both, to tell an employee who to contact.

   ![The Departments page](/guide-images/admin-departments.png)

   *The Departments page after clicking Load default departments. Each department shows what it handles and its contact.*
2. **Users.** Open **Users** and click **Load default users**. We enter one password for all of them, and the 15 default users are divided equally over the departments. We can also add users one by one with **Add user**. Every user belongs to a department, so this is only possible after step 1.
3. **Knowledge.** Open **Knowledge**, choose a department, and upload its knowledge files (see section 5 for the file format). This is what the assistant answers from, and nothing works well without it.

   ![The Knowledge page](/guide-images/admin-knowledge.png)

   *The Knowledge page. We choose the department and the files, pick Add or Flush and Add, and click Add. Below, the stored sections are listed in tabs by department.*
4. **Settings.** On the AI assistant tab, we click **Restore defaults** once. Nothing is saved at startup, so until then the app uses the built-in defaults; this saves them as settings and picks the chat and embedding model from the keys in `.env` (Gemini with `GEMINI_API_KEY`, otherwise Ollama). Section 6 explains the model settings.

## 4. Use the app as an employee

**Log in** with the username and the password the admin gave us. A round chat button appears at the bottom right of every page.

**Ask a question.** We click the chat button and type a question, for example "how many leave days do I get?". The assistant answers from the company knowledge. Casual messages like "hello" get a short friendly reply, and requests that are not about work are politely declined.

![The chat window while the assistant is answering](/guide-images/employee-chat-waiting.png)

*The chat window. The assistant shows a typing indicator while it searches the knowledge and writes the answer.*

**When the answer does not help.** If the assistant answers a problem, it ends with an offer to create a ticket. A conversation looks like this:

1. We write: `i am not able to reset my password`
2. The assistant explains how to reset it and says it can create a ticket if that does not solve the issue.
3. We write: `that did not work`
4. The assistant offers to create a ticket for the right department and asks if we want it.
5. We write: `yes`
6. The assistant creates the ticket and tells us the ticket number and who it is assigned to.

Tickets are assigned to the members of the department one by one, in turn, so the work is shared. The assistant answers in 15 to 30 seconds on a computer without a GPU, because the model reads the knowledge before it writes. The window shows a typing indicator while it works.

![A ticket created from the chat and the Tickets page](/guide-images/employee-ticket-created.png)

*A problem report in the chat. After we say yes, the assistant creates the ticket, and it shows on the Tickets page under Reported by you.*

**Tickets.** The **Tickets** page has two tabs:
- **Reported by you:** the tickets we reported, with their status.
- **Assigned to you:** the tickets given to us. Here we change the status in the dropdown: **New**, **In progress** or **Completed**. Only the person a ticket is assigned to can change its status.

## 5. Use the app as an admin

The admin has these pages. The admin does not report or receive tickets, and can see all of them.

| Page | What we do there |
| --- | --- |
| **Tickets** | See every ticket, who reported it, who it is assigned to, and its status |
| **Knowledge** | Upload knowledge files, remove a department's knowledge, and see all stored sections in tabs by department |
| **Departments** | Add, edit and delete departments. A department that still has users cannot be deleted |
| **Users** | Add, edit and delete users, and change their department |
| **Settings** | Change the models, the prompt, and how the assistant searches. **Restore defaults** puts everything back |
| **Docs** | The developer documentation about the AI (this is open to everyone, without login) |

**Uploading knowledge.** Each file starts with a title and a department, then one or more sections:

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement
Full-time employees receive 18 days of paid annual leave per year...

## Sick leave
Employees can take up to 12 days of paid sick leave per year...
```

- We choose the department in the form, and the department in the file must be the same. If it is different, the file is discarded and the page says so. A file in the wrong format is rejected with a message that says what to fix. We can upload several files at once, and each file gets its own result.
- **Add** keeps the existing knowledge. A file with the same name replaces its earlier sections.
- **Flush and Add** deletes all existing knowledge first, and then adds the uploaded files.
- **Remove knowledge** (next to the file picker) deletes all the knowledge of the selected department.
- Each section is one piece the assistant can find, so it should make sense on its own and name its topic. Sample files are in `server/src/TicketingAI.Api/Knowledge/Documents`.

## 6. Change the AI model

The default models are `qwen2.5:7b` for answers and `nomic-embed-text` for turning text into numbers. A smaller model such as `qwen2.5:3b` is faster and needs about half the memory, but its answers should be tested again, because the prompt was tuned on the 7B model.

To change the chat model, two things must change: the model has to exist in Ollama, and the app has to be told to use it.

![The Settings page](/guide-images/admin-settings.png)

*The Settings page. Chat model, Messages of history and Max answer length are edited here, and Restore defaults is at the top right.*

**With Docker**
1. In `docker-compose.yml`, in the `pull-models` service, we change `ollama pull qwen2.5:7b` to `ollama pull qwen2.5:3b` in the `entrypoint` line, and run `docker compose up -d`. For a one-off test, `docker compose exec ollama ollama pull qwen2.5:3b` does the same.
2. In the app, we open **Settings**, edit **Chat model**, and set `qwen2.5:3b`. It applies to the next message, with no restart.

**Without Docker:** we run `ollama pull qwen2.5:3b`, then do step 2.

**Restore defaults** on the Settings page puts the model back to `qwen2.5:7b`.

**Use an external API instead (Gemini, Cloudflare).** The chat answers can also come from an online service instead of a local model. Embeddings stay on Ollama. These are all settings on the **Settings** page (AI assistant tab), and nothing about the AI assistant, except the key, is in `appsettings.json`. For a **new** database, `AiDefaults__ChatProvider`, `AiDefaults__ChatBaseUrl` and `AiDefaults__ChatModel` in `.env` can set the starting values, and they are what Restore defaults restores. For a database that already exists, we use the Settings page. Three steps:
1. We add the service's key to the `.env` file under its own variable (`GEMINI_API_KEY`, `GROQ_API_KEY`, ...; `.env.example` lists them). The same file works for Docker and for running without Docker. The Settings page shows the **API key name** of each model and whether it is set, and saves only the name. We then restart the API, or run `docker compose up -d`. The key is never stored in the database and never goes in `appsettings.json`.
2. In **Settings**, AI assistant tab, we click **Use** on the service (or on a model we saved). This sets the **Chat API base URL** and the **Chat model** together. **Edit** on a model (a saved one, a service, or **In use now**) changes its address, model and key name; editing a service saves a copy as our own model, and editing the model in use tests it and switches to it. The address, the model and the key name are not shown again as separate settings. There is no separate provider setting: an address with no path is Ollama, and one with a path is an online service.
3. Which models a key can use differs from key to key. If the chat says a model was not found, we click **View models** on the service's row. A dialog lists the models the service reports, each with a **Test** button (it sends one tiny request and saves nothing; a failure is shown in a second dialog above the list) and a **Select** button, which sets the address, the model and the key name together. A service can list models our key cannot use, so we test a model before we select it. To look for a fast one, we use **Next model** or **Start from the beginning** at the top of the chat model box: they go through the chat models of every service we have a key for, test each one, and stop at the first that answers, with its response time. **Select** uses it, **Next** tries the next one, and a model that fails is skipped by itself. We send a test message.

| | Gemini | Cloudflare Workers AI |
| --- | --- | --- |
| Chat API base URL | `https://generativelanguage.googleapis.com/v1beta/openai` | `https://api.cloudflare.com/client/v4/accounts/<ACCOUNT_ID>/ai/v1` |
| Chat model (example) | `gemini-3.8-flash` | `@cf/meta/llama-3.1-8b-instruct-fp8` |

To go back to the local model, we click **Use** on "Ollama (on our own machine)", or set **Chat API base URL** to `http://localhost:11434` (`http://ollama:11434` in Docker) and **Chat model** to `qwen2.5:7b`. Models we use often can be saved with **Add chat model**, to switch with one click. The prompt was tuned on `qwen2.5:7b`, so we re-test after switching. The questions and the knowledge sections are sent to the online service, so we avoid real company data on a free tier.

The **embedding model** is chosen the same way, in the Embedding group of the Settings page. It is different in one thing: numbers from two different models cannot be compared, so **Use converts all the stored knowledge to the new model** (every section is embedded again). That takes about a minute, and the change is only saved when it worked, so a wrong key changes nothing. The model's prefixes are set together with it.

**Making replies faster.** On the Settings page we can lower **Results to retrieve** (4 by default), raise **Maximum score gap** carefully so fewer sections are sent (a shorter prompt is a faster reply), lower **Messages of history** (3 by default; 1 sends only the last message and still works for a ticket) and **Max answer length** (2000 by default; keep it generous, because a model that thinks counts its thinking in it, and then the chat says the reply was cut off). For a hosted model that thinks before it answers (such as the newer Gemini models), **Reasoning effort** can be set to `none`, `minimal` or `low`, depending on what the model accepts. The server log has a timing line for every message that shows where the time goes.

## 7. If something goes wrong

| Problem | What to do |
| --- | --- |
| Docker: login says "something went wrong", and the server keeps restarting with `table ... already exists` | An old database in the `server-data` volume does not match. Run `docker compose down`, then `docker volume rm ticketing-ai_server-data`, then `docker compose up --build` |
| `docker compose up` fails, or the server stops at startup and says that `Admin:Password` or `Jwt:Key` is required | The `.env` file is missing or incomplete. We copy `.env.example` to `.env` and fill it in |
| Chat or upload says the model was not found | The model in Settings is not in Ollama. We pull it first (section 6) |
| "Cannot reach the server" | The API is not running, or in Docker it is still starting or has crashed. `docker compose logs server` shows why |
| Replies take 15 to 30 seconds | This is normal without a GPU. The model reads the knowledge before it writes the first word |
| Docker: a reply is very slow, or the model is killed | Docker Desktop has too little memory. We raise it to at least 10 GB |
| The assistant says it has no information about everything | Nothing has been uploaded yet. We upload the files |
| We want to start from zero | `docker compose down -v --remove-orphans`, then `docker compose build --no-cache`, then `docker compose up`. This also deletes the downloaded models |
