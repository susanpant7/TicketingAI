# How to run and use the app

Ticketing AI is a support app for a company. Employees chat with an AI assistant that answers from the company's own knowledge. If the answer does not solve the problem, the assistant can create a ticket and assign it to someone in the right department. This page explains how to run the app, how to set it up the first time, and how to use it. How the AI works is on the [AI and RAG](/docs?tab=ai-rag) tab.

**The code is on GitHub:** [github.com/susanpant7/TicketingAI](https://github.com/susanpant7/TicketingAI). To get it:

```bash
git clone https://github.com/susanpant7/TicketingAI.git
cd TicketingAI
```

All the commands below are run from this project folder.

**Tech stack.**

| Part | What we use |
| --- | --- |
| Client | React 19 and TypeScript, built with Vite, styled with Tailwind |
| API | .NET 10 Web API (ASP.NET Core), with Entity Framework Core |
| Database | **SQLite** (one file). The app currently uses SQLite for everything: users, tickets, settings and the knowledge. The vector search runs inside it, with the sqlite-vec extension |
| AI models | Ollama on our own computer, or online services (Gemini, Groq, ...), chosen on the Settings page |

## 1. Run the app

**What we need:** the .NET 10 SDK, Node.js 24 with npm, and [Ollama](https://ollama.com/download), the program that runs the AI models on our own computer (the default). The key of an online service is optional.

**Step 1. Start Ollama and download the models** (about 5 GB, only once). This is the default for the chat and the embeddings.

```bash
ollama serve                  # not needed if the Ollama desktop app is already running
ollama pull qwen2.5:7b        # writes the answers
ollama pull nomic-embed-text  # turns text into numbers
```

**Step 2. Create the secrets file.** We copy the example and edit it (the admin password, `Jwt__Key`, and the chat API key if we use one):

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

**Step 5. Open the app.** We go to http://localhost:5173 and log in with `Admin__Username` and `Admin__Password` from `.env`. Then we follow section 2 below.

## 2. Set up the app for the first time

A new database is empty. We log in as the admin and open **Settings**, then the **Manage App Data** tab.

![The Manage App Data tab of the Settings page](/guide-images/admin-manage-app-data.png)

*The Manage App Data tab. One button loads all the default data, and each part also has its own button.*

1. **Load all the default data.** We click **Load all default data**. It cleans up, then loads the AI assistant settings, the departments (IT, HR and Finance) and the default users, who all get the password we enter. It does not load the knowledge files, and it does not select a chat or an embedding model.
2. **Choose how the AI models run.**
   - **To use the AI models locally,** we run them on our own computer with Ollama (see section 1).
   - **To use the AI models with APIs,** we get an API key from the service. The free AI models are listed on the **AI assistant** tab: we get a key from the service and put it in the `.env` file (the variable name is shown on the tab, for example `GEMINI_API_KEY`), then restart the API.
3. **Select an embedding model and load the knowledge data.** On the **AI assistant** tab, in the Embeddings part, we click **View models** and **Select** a model. The stored knowledge is cleaned and inserted again with it, and when there is none, the default knowledge files are loaded. A red message at the top of the Settings page says "The app does not have any knowledge files" until that is done.
4. **Select an AI chat model.** In the chat part of the same tab, we click **View models** and **Select** a model.
5. **If the chat model does not work,** we click **Next model** or **Search model from the beginning**. This tests the models one after another and finds the next working one. Then we click **Select** to use it.

Now everything is set up to use RAG with the AI model. Section 5 has more about the model settings.

**The Knowledge page.** All the knowledge the AI knows about our application can be viewed on the **Knowledge** page. Every stored section is listed there, in tabs by department, with its document and category. On the same page we can:

- **Add knowledge files for a department.** We choose the department and the files (`.md` or `.txt`, in the format in section 4), pick **Add** or **Flush and Add**, and click **Add**. **Remove knowledge** deletes all the knowledge of the selected department.
- **Write one piece of knowledge ourselves.** The **Write knowledge** button opens a form with a title, a department and one or more sections (a name and the details). It is a way to add a single knowledge chunk without writing a file.
- **Test a question.** **Test a question** shows which sections the assistant would find for it, with their scores.

![The Knowledge page](/guide-images/admin-knowledge.png)

*The Knowledge page. We choose the department and the files and click Add, or click Write knowledge at the top right. Below, the stored sections are listed in tabs by department.*

![The Write knowledge dialog](/guide-images/admin-write-knowledge.png)

*The Write knowledge dialog. A title, a department and one or more sections; each section is one piece the assistant can find.*

## 3. Use the app as an employee

An employee can log in to the application, and then ask any question or ask for help with an issue. The AI replies based on the knowledge data that the admin loaded (the company's own documents), so the answers follow what the company really says. A round chat button at the bottom right of every page opens the chat. To log in, we use the username and the password the admin gave us.

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

## 4. Use the app as an admin

The admin has these pages. The admin does not report or receive tickets, and can see all of them.

| Page | What we do there |
| --- | --- |
| **Tickets** | See every ticket, who reported it, who it is assigned to, and its status |
| **Knowledge** | Upload knowledge files, remove a department's knowledge, and see all stored sections in tabs by department |
| **Departments** | Add, edit and delete departments. A department that still has users cannot be deleted |
| **Users** | Add, edit and delete users, and change their department |
| **Settings** | Change the models, the prompt, and how the assistant searches. **Load default AI assistant data** (Manage App Data tab) puts the settings back |
| **Dev Docs** | The installation guide and the AI and RAG documentation (open to everyone, without login) |

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

## 5. Change the AI model

Everything about the AI that we can change is on the **AI assistant** tab of the Settings page, including the AI models: the chat model, the embedding model, how the knowledge is searched, and how long the answers can be. A new database has no chat or embedding model selected, so we choose them here.

![The chat model part of the AI assistant tab](/guide-images/admin-ai-models.png)

*The chat model part of the AI assistant tab. The model in use is at the top, then the saved models, then the services we can use.*

**Next model and Search model from the beginning.** A model can stop working (the service retired it, a limit was reached, the key cannot use it). These two buttons find another model that works, without trying them by hand:

- **Next model** tests the models one after another, starting after the model in use, and stops at the first one that answers a small test question. It shows how long the answer took.
- **Search model from the beginning** does the same, but starts at the first model of the first service (after a confirmation).

When a model answers, we click **Select** to use it, or **Next** to look further. A model that gives an error is skipped by itself, and the reason is listed. Only the services we have a key for are tried.

**View models, Test and Select.** To pick a model ourselves, we click **View models** on the model in use, on a saved model, or on a service. The dialog lists the models the service offers for our key, with the model in use first.

![The list of models of a service](/guide-images/admin-view-models.png)

*The models of Google Gemini. Test sends one small request and shows the answer time (or the error). Select uses the model.*

- **Test** sends one small question to that model and saves nothing. It shows how long the model took, or why it failed. A service can list models our key cannot use, so we test before we select.
- **Select** uses the model from the next message, with no restart. It tests the model first, so a model that does not work is never saved.

For an online service, the key in `.env` is enough: nothing has to be downloaded. For a model on our own computer (Ollama), the model must be pulled there first (`ollama pull qwen2.5:3b`). To go back to our own computer, we click **View models** on "Ollama (on our own machine)" and **Select** the model. Models we use often can be saved with **Add chat model**, and **Edit** changes the address, the model or the key name. The questions and the knowledge sections are sent to an online service, so we avoid real company data on a free tier.

The **embedding model** is chosen the same way, in the Embedding group of the Settings page. It is different in one thing: numbers from two different models cannot be compared, so **Select cleans the stored knowledge and inserts it again with the new model** (every section is embedded again). That takes about a minute, and the change is only saved when it worked, so a wrong key changes nothing. The two prefixes put in front of the texts are the same for every model and are not settings.

**Using the Ollama on our computer for the embeddings, from a server.** A server such as Render cannot reach `localhost` on our computer. A **Cloudflare Tunnel** gives the Ollama on our computer a public https address, so the server can use it for the embeddings. The domain must be on Cloudflare, and `cloudflared` must be installed (`brew install cloudflared`).

1. **Pull the embedding model** on our computer, and make sure Ollama is running:

   ```bash
   ollama pull nomic-embed-text
   curl http://localhost:11434/api/tags
   ```

2. **Create the tunnel and its address.** We use a long random name, because there is no login on this address and anyone who knows it can use our Ollama. The second command adds the DNS record in Cloudflare by itself.

   ```bash
   cloudflared tunnel login
   cloudflared tunnel create ollama
   cloudflared tunnel route dns ollama <my-ollama>.susanpant.com.np
   ```

3. **Run the tunnel.** Ollama only accepts requests for its own host name, so the Host header is rewritten. We leave this terminal open:

   ```bash
   cloudflared tunnel run --url http://localhost:11434 --http-host-header localhost:11434 ollama
   ```

   In a second terminal we check it. The models of our computer must be listed:

   ```bash
   curl https://<my-ollama>.susanpant.com.np/api/tags
   ```

4. **Add it in the app.** On Settings, AI assistant tab, in the Embeddings part, we click **Edit** on "Ollama on our computer, from a server". We put our address in it, with no path, for example `https://<my-ollama>.susanpant.com.np` (not `.../api/tags`). The model is `nomic-embed-text` and the API key name stays empty. We save, then click **View models** on the new saved model and **Select** `nomic-embed-text`. The stored knowledge is cleaned and inserted again through our computer.

Ollama and the tunnel must stay running, and the computer must be awake, because every question is embedded there. If they are off, the chat and the knowledge upload fail with "Cannot reach Ollama". The chat model is separate: it can still be an online service. To stop sharing the address, we run `cloudflared tunnel delete ollama` and remove the DNS record in Cloudflare.

**Making replies faster.** On the Settings page we can lower **Results to retrieve** (4 by default), raise **Maximum score gap** carefully so fewer sections are sent (a shorter prompt is a faster reply), lower **Messages of history** (3 by default; 1 sends only the last message and still works for a ticket) and **Max answer length** (2000 by default; keep it generous, because a model that thinks counts its thinking in it, and then the chat says the reply was cut off). For a hosted model that thinks before it answers (such as the newer Gemini models), **Reasoning effort** can be set to `none`, `minimal` or `low`, depending on what the model accepts. The server log has a timing line for every message that shows where the time goes.

## 6. If something goes wrong

| Problem | What to do |
| --- | --- |
| Login says "something went wrong", and the server stops with `table ... already exists` | An old database does not match. We stop the API, delete the file `server/src/TicketingAI.Api/data/ticketing.db*`, and start again |
| The server stops at startup and says that `Admin:Password` or `Jwt:Key` is required | The `.env` file is missing or incomplete. We copy `.env.example` to `.env` and fill it in |
| Chat or upload says the model was not found | The model in Settings is not in Ollama. We pull it first (section 5) |
| "Cannot reach the server" | The API is not running or has crashed. Its terminal shows why |
| Replies take 15 to 30 seconds | This is normal without a GPU. The model reads the knowledge before it writes the first word |
| The assistant says it has no information about everything | Nothing has been uploaded yet. We upload the files |
| We want to start from zero | We stop the API and delete the database file (`server/src/TicketingAI.Api/data/ticketing.db*`). It is created again at the next start |
