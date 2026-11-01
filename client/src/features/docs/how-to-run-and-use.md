# How to run and use the app

Ticketing AI is a support app for a company. Employees chat with an AI assistant that answers from the company's own knowledge. If the answer does not solve the problem, the assistant can create a ticket and assign it to someone in the right department. This page explains how to run the app, how to set it up the first time, and how to use it. How the AI works is on the [Docs](/docs) page.

**The code is on GitHub:** [github.com/susanpant7/TicketingAI](https://github.com/susanpant7/TicketingAI). To get it:

```bash
git clone https://github.com/susanpant7/TicketingAI.git
cd TicketingAI
```

All the commands below are run from this project folder.

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

The same data can also be added by hand, page by page:

- **Departments.** On **Departments** we click **Load default departments**, or add our own with **Add department**. Each department has a description of what it handles and a contact. The assistant uses both, to tell an employee who to contact.

  ![The Departments page](/guide-images/admin-departments.png)

  *The Departments page after clicking Load default departments. Each department shows what it handles and its contact.*
- **Users.** On **Users** we click **Load default users**. We enter one password for all of them, and the 15 default users are divided equally over the departments. We can also add users one by one with **Add user**. Every user belongs to a department, so this is only possible after the departments are loaded.
- **Knowledge.** On **Knowledge** we choose a department and upload its knowledge files (see section 4 for the file format). This is what the assistant answers from, and nothing works well without it.

  ![The Knowledge page](/guide-images/admin-knowledge.png)

  *The Knowledge page. We choose the department and the files, pick Add or Flush and Add, and click Add. Below, the stored sections are listed in tabs by department.*

## 3. Use the app as an employee

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

## 4. Use the app as an admin

The admin has these pages. The admin does not report or receive tickets, and can see all of them.

| Page | What we do there |
| --- | --- |
| **Tickets** | See every ticket, who reported it, who it is assigned to, and its status |
| **Knowledge** | Upload knowledge files, remove a department's knowledge, and see all stored sections in tabs by department |
| **Departments** | Add, edit and delete departments. A department that still has users cannot be deleted |
| **Users** | Add, edit and delete users, and change their department |
| **Settings** | Change the models, the prompt, and how the assistant searches. **Load default AI assistant data** (Manage App Data tab) puts the settings back |
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

## 5. Change the AI model

A new database has no chat or embedding model selected: we choose them on the Settings page. For a model on our own computer, `qwen2.5:7b` answers and `nomic-embed-text` turns text into numbers. The prompt was tuned on `qwen2.5:7b`, so we re-test the ticket flow after switching to another model.

To change the chat model, we use **View models** (then **Select**), **Edit** or **Next model** on the Settings page. For an online service, the key in `.env` is enough: nothing has to be downloaded. For a model on our own computer (Ollama), the model must be pulled there first (`ollama pull qwen2.5:3b`), and then we **Select** it. It applies to the next message, with no restart.

![The Settings page](/guide-images/admin-settings.png)

*The Settings page. Chat model, Messages of history and Max answer length are edited here.*

To go back to a model on our own computer, we click **View models** on "Ollama (on our own machine)" and **Select** the model, or **Edit** the model in use and set the address to `http://localhost:11434` and the model to `qwen2.5:7b`. Models we use often can be saved with **Add chat model**, to switch with one click. The prompt was tuned on `qwen2.5:7b`, so we re-test after switching. The questions and the knowledge sections are sent to the online service, so we avoid real company data on a free tier.

The **embedding model** is chosen the same way, in the Embedding group of the Settings page. It is different in one thing: numbers from two different models cannot be compared, so **Use converts all the stored knowledge to the new model** (every section is embedded again). That takes about a minute, and the change is only saved when it worked, so a wrong key changes nothing. The two prefixes put in front of the texts are the same for every model and are not settings.

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
