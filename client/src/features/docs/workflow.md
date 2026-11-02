# Workflow: how the app works

This tab follows the app from the beginning, as a developer would: how the **knowledge data** is prepared, how it is loaded into a table and embedded, how new knowledge is added later, and then a real chat: it starts with `hello`, goes on with a question about the company, then an issue, and ends with the **ticket** being created. Every step shows the request and the response.

The concepts (what a vector or RAG is) are on the **AI and RAG** tab. Ids, similarity scores, names and the model's wording in the examples are **illustrative**, because the wording varies from run to run. The field names, the endpoints and the shapes are what the code really sends and receives.

## 1. The big picture

RAG (retrieval-augmented generation) means the model does not answer from memory. The app first **retrieves** the company text that is relevant to the question, puts it in the prompt, and asks the model to answer **only from that text**.

There are two separate pipelines. The first runs when an admin uploads knowledge. The second runs on every chat message.

```
INGEST (when an admin uploads a file)

  file ──► split into sections ──► embed each section ──► save text + vector in KnowledgeChunks
           (## headings)           (Ollama /api/embed)


ANSWER (on every chat message)

  message ──► embed the message ──► find the closest vectors ──► build the prompt ──► model answers ──► parse the reply
              (Ollama /api/embed)   (sqlite-vec, top 5)          (sections + history)  (Ollama /api/chat)  (answer, department, ticket)
```

| Piece           | What it is                                                                                                                                       |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Embedding model | `nomic-embed-text`. Turns a text into a list of 768 numbers (a **vector**). Texts with similar meaning get similar vectors                       |
| Chat model      | `qwen2.5:7b`. Reads the prompt and writes the answer                                                                                             |
| Vector store    | The `KnowledgeChunks` table in SQLite, with the **sqlite-vec** extension for the similarity query. There is no separate vector database          |
| Ollama          | Runs the models on our own computer, when we use local models. It is optional: the models can be online services, and Docker does not include it |

Both model names, and every number that tunes the behaviour, are rows in the `Settings` table (see [section 14](#14-settings-that-tune-the-ai)). The app reads them on every request.

## 2. Preparing the knowledge data

The knowledge data is a set of Markdown (`.md`) or text files. **One file is one topic**, and it belongs to **one department**. The assistant answers only from what is in these files.

**Where the files come from.** An admin writes them, or uploads them on the **Knowledge** page. The repository has 37 sample files for HR, IT and Finance (set in Nepal) in `server/src/TicketingAI.Api/Knowledge/Documents`, with the format notes and about 83 test questions in `server/src/TicketingAI.Api/Knowledge/README.md`.

**The format.**

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement

Full-time employees receive 18 days of paid annual leave per year. Leave is counted from the first day of the Nepali fiscal year.

## Sick leave

Employees can take up to 12 days of paid sick leave per year. A medical certificate is needed for more than 2 days in a row.
```

- `title:` and `department:` come first. The department must be one that exists, and the same one the admin selected in the upload form.
- Every `## ` line starts a **section**. A section is the unit that is stored, searched and given to the model: **one row in the table**.
- A file is rejected, with a message for that file, when it has no title or department, text before the first section, no sections, an empty or repeated section title, an empty section, a size above 1 MB, or a department that is not the selected one.

**How to write good sections.** The model reads the sections that were found, and nothing else, so a section must make sense on its own:

- One topic per section, and name the topic in the text. Do not rely on the heading above it.
- Put the exact facts in: numbers, limits, dates, names, emails, links. The assistant repeats them as they are written.
- Keep sections short. A short section about one topic is found much more reliably than a long one about many.

When the file is uploaded, the app splits it at the `## ` lines. The file above becomes two sections:

| Section | Heading (title) | Content (text) |
| --- | --- | --- |
| 1 | Annual leave entitlement | Full-time employees receive 18 days of paid annual leave per year. Leave is counted from ... |
| 2 | Sick leave | Employees can take up to 12 days of paid sick leave per year. A medical certificate is needed ... |

## 3. The table that holds the knowledge

The knowledge lives in one normal table, `KnowledgeChunks`, with one extra column, `Embedding`, that holds the vector. In SQL it is:

```sql
CREATE TABLE KnowledgeChunks (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    DocumentTitle TEXT NOT NULL,       -- title from the file
    Category      TEXT NOT NULL,       -- the department the file belongs to
    Heading       TEXT NOT NULL,       -- the section title
    Content       TEXT NOT NULL,       -- the section text (what the chat model reads)
    SourceFile    TEXT NOT NULL,       -- the file name
    ContentHash   TEXT NOT NULL,       -- SHA-256 of Content
    Embedding     BLOB NOT NULL,       -- the vector: 768 float32 values, 3072 bytes
    IngestedAt    TEXT NOT NULL
);
CREATE UNIQUE INDEX IX_KnowledgeChunks_SourceFile_Heading ON KnowledgeChunks (SourceFile, Heading);
CREATE INDEX IX_KnowledgeChunks_Category ON KnowledgeChunks (Category);
```

- We create the table with an EF Core migration, not by hand. Reference: `server/src/TicketingAI.Data/Entities/KnowledgeChunk.cs` and `Persistence/Configurations/KnowledgeChunkConfiguration.cs`.
- `Category` is the **department name**. That is what ties a section to a department.
- We read the `Embedding` column only when we search. The list screens do not select it.

**The vector functions.** The database is SQLite. The **sqlite-vec** extension (the NuGet package `sqlite-vec`) adds `vec_distance_cosine`, the function that compares two vectors. `SqliteVecConnectionInterceptor` loads it into every connection EF Core opens, and `Program.cs` runs this at startup, so a broken install fails at once and not at the first chat:

```sql
SELECT vec_version();
```

The migrations also run at startup (`Database.Migrate()`). Nothing else is written then: the departments, users and knowledge are loaded later by an admin.

## 4. Loading the knowledge: splitting and embedding

An admin opens the **Knowledge** page, selects a department and the files, and clicks **Add**. The page sends the files as a form (this needs the admin token):

```http
POST /api/knowledge/upload
Authorization: Bearer <admin token>
Content-Type: multipart/form-data

departmentId = 2
flush        = false
files        = leave-policy.md
```

`flush=false` is **Add**: a file with the same name replaces its earlier sections. `flush=true` is **Flush and Add**: all the existing knowledge is deleted first (only if at least one file is valid).

The server does this for every file:

**1. Parse and validate** (`KnowledgeUploadParser`). It reads the title and the department, and cuts the file at every `## ` line (section 2).

**2. Build the text to embed** for each section. The title and the heading are put in front of the content, so a short section still carries its topic, and the label `search_document:` is put before it (the embedding model was trained with it; questions get `search_query:`):

```
search_document: Leave Policy - Annual leave entitlement
Full-time employees receive 18 days of paid annual leave per year...
```

**3. Embed** the texts, 16 at a time. For Ollama (an address with no path) the request is:

```http
POST {embedding.base_url}/api/embed
Content-Type: application/json

{
  "model": "nomic-embed-text",
  "input": [
    "search_document: Leave Policy - Annual leave entitlement\nFull-time employees receive 18 days ...",
    "search_document: Leave Policy - Sick leave\nEmployees can take up to 12 days of paid sick leave ..."
  ]
}
```

```json
{
  "embeddings": [
    [0.0123, -0.0456, 0.1012, ...768 numbers],
    [0.0311,  0.0052, -0.0874, ...768 numbers]
  ]
}
```

One vector comes back for each text, in the same order. For an online service (an address with a path, such as Gemini's) the same texts go to `POST {embedding.base_url}/embeddings` with the body `{ "model": "...", "input": [...] }` and the header `Authorization: Bearer <key>`, and the vectors come back in `data[i].embedding`. The key is read from the environment variable named in the setting, and is never stored.

**4. Store one row per section** in `KnowledgeChunks`:

| Column | Value |
| --- | --- |
| `DocumentTitle`, `Heading`, `Content` | From the file. `Content` has no prefix |
| `Category` | The department name |
| `SourceFile` | The uploaded file name |
| `ContentHash` | SHA-256 of the content |
| `Embedding` | The 768 numbers as raw **float32** values in a blob (3072 bytes) |
| `IngestedAt` | UTC time |

All files are validated and embedded **before** anything is saved, so if the embedding model fails, the existing knowledge stays as it was.

**The response** has one item per file:

```json
[
  { "fileName": "leave-policy.md", "sections": 2, "error": null },
  { "fileName": "broken.md", "sections": 0, "error": "The file has no title. Start it with a title: line." }
]
```

The second item shows a rejected file: the others are still added. The same pipeline runs when we click **Load default knowledge files** (`POST /api/knowledge/load-defaults`), which uploads the 37 sample files into their departments.

## 5. Adding or changing knowledge later

There is no training and no restart. Changing the table is enough: the next chat message already searches the new rows. There are four ways to change it, and all of them end in the same pipeline (section 4).

**1. Upload more files.** The same request as in section 4. A file with the same name replaces its earlier sections, so an edited file is uploaded again.

**2. Write a document on the page.** The **Write knowledge** button opens a form with a title, a department and sections, so a single piece of knowledge needs no file:

```http
PUT /api/knowledge/documents
Authorization: Bearer <admin token>

{
  "sourceFile": null,
  "title": "Laptop and hardware",
  "departmentId": 1,
  "sections": [
    {
      "heading": "Requesting a new laptop",
      "content": "Employees request a new laptop from IT at it-help@ticket.com. New laptops are delivered within 5 working days."
    }
  ]
}
```

```json
{ "fileName": "laptop-and-hardware.md", "sections": 1, "error": null }
```

`sourceFile` is `null` for a new document, and the server makes the file name from the title (a title that is already taken is answered with `409`). **Edit document**, **Add section**, and **Edit** or **Delete** on one section send the same request, with the `sourceFile` of the document and its full list of sections. The server writes the document in the upload format (the `title:`, `department:` and `## ` lines of section 2) and sends it through exactly the same checks and steps as an uploaded file (section 4). A document with the same file name is replaced.

**3. Delete knowledge.**

```http
DELETE /api/knowledge/documents/laptop-and-hardware.md     (one document)
DELETE /api/knowledge/department/1                         (all the knowledge of a department)
```

```json
{ "deleted": 1 }
```

**4. Change the embedding model.** Vectors from two different models cannot be compared, so the stored knowledge must be embedded again with the new model. **Select** on an embedding model does it, in one go:

```http
POST /api/settings/use-embedding-model
Authorization: Bearer <admin token>

{ "baseUrl": "http://localhost:11434", "model": "nomic-embed-text", "apiKeyName": "" }
```

```json
{ "reembedded": 270, "error": 0, "message": null }
```

The server embeds the text of every stored section with the new model first, and only then saves the new settings and the new vectors in **one transaction**. If the model or the key is wrong, nothing is changed. When there is no knowledge yet, `reembedded` is `0`, and the page then loads the default knowledge files.

## 6. A user logs in and starts chatting: "hello"

**Login.** An employee logs in, and the server returns a signed token:

```http
POST /api/auth/login

{ "username": "meera", "password": "..." }
```

```json
{ "username": "meera", "role": "User", "token": "eyJhbGciOi..." }
```

The page keeps the token. Every request after this sends it as `Authorization: Bearer <token>`, and the server reads the employee id from it. A round chat button appears at the bottom right of every page. The admin signs in the same way (from configuration), but is not an employee, so the admin cannot report or receive tickets.

**The first message.** The employee opens the chat and types `hello`. The page sends the message, and the earlier messages of the conversation (none yet), because the server keeps no conversation of its own:

```http
POST /api/chat/stream
Authorization: Bearer <employee token>
Content-Type: application/json

{ "message": "hello", "history": [] }
```

What the server does, in order:

**1. Read the settings and the departments.** The settings come from the `Settings` table and the departments from the `Departments` table, on every request, so a change on the Settings page applies to the next message.

**2. Build the search query.** `hello` has one word. A message with 5 words or fewer is searched together with the user's previous message when there is one (a short follow-up such as "and casual leave?" needs it), and there is none here, so the query is `hello`.

**3. Embed the query and search.**

```http
POST {embedding.base_url}/api/embed

{ "model": "nomic-embed-text", "input": ["search_query: hello"] }
```

```sql
SELECT Id, DocumentTitle, Category, Heading, Content, SourceFile,
       vec_distance_cosine(Embedding, @queryVector) AS Distance
FROM KnowledgeChunks ORDER BY Distance LIMIT 5;
```

Nothing in the company knowledge is about greetings, so every score is below `retrieval.min_score` (0.5), and all the rows are dropped.

**4. Build the prompt.** With no sections left, the message of the user is this:

```
Sections:

No company documents matched this message.

Message: hello

(Begin your reply with the Department line. ...)
```

It is sent after the system prompt, which defines the six kinds of message (section 11). The prompt, not the code, decides what to do with a greeting.

**5. Ask the model.** For Ollama:

```http
POST {chat.base_url}/api/chat

{ "model": "qwen2.5:7b", "stream": true, "options": { ... }, "messages": [ { "role": "system", "content": "..." }, { "role": "user", "content": "Sections:\n\nNo company documents matched this message.\n\nMessage: hello\n\n(...)" } ] }
```

The model sees casual chat (kind 3) and answers with a **header line**, a blank line, and the reply:

```
Department: none

Hello! How can I help you today?
```

**6. Parse the reply.** `Department: none` means no department, and the header line is removed from what the employee sees. There is no `Problem: yes`, so no ticket is offered.

**The response** is sent as it is written, one JSON object per line. `draft` lines hold the reply so far (header lines are never shown), and the last line is the final answer:

```
{"draft":"Hello! How can"}
{"draft":"Hello! How can I help you today?"}
{"reply":{"answer":"Hello! How can I help you today?","department":null}}
```

If the model or the embedding service is not available, the last line is `{"error":"..."}` with a clear message, for example that no chat model is selected yet.

## 7. A question about the company

The employee asks: `How many leave days do I get?`

```http
POST /api/chat/stream
Authorization: Bearer <employee token>

{
  "message": "How many leave days do I get?",
  "history": [
    { "role": "user", "content": "hello" },
    { "role": "assistant", "content": "Hello! How can I help you today?" }
  ]
}
```

**1. Search.** The message has 7 words, more than 5, so it is the search query as it is. It is embedded with the `search_query:` label, and the five closest sections come back with their scores (`1 - distance`):

| Section | Department | Score |
| --- | --- | --- |
| Leave Policy - Annual leave entitlement | HR | 0.78 |
| Leave Policy - Sick leave | HR | 0.62 |
| Leave Policy - Casual leave | HR | 0.60 |
| Payroll - Salary payment dates | Finance | 0.41 |
| Holidays - Public holidays | HR | 0.40 |

**2. Filter.** Sections under `retrieval.min_score` (0.5) are dropped, and so are sections more than `retrieval.max_score_gap` (0.05) below the best one. Only `Annual leave entitlement` stays: fewer sections make a shorter prompt, and a shorter prompt is a faster reply.

**3. The prompt.** The last message to the model holds the section, with a label made of the document title, the heading and the department:

```
Sections:

[Leave Policy - Annual leave entitlement | HR]
Full-time employees receive 18 days of paid annual leave per year. Leave is counted from the first day of the Nepali fiscal year.

Message: How many leave days do I get?

(Begin your reply with the Department line. ...)
```

**4. The model's reply** (kind 1, a question the sections answer). It uses the exact numbers, and labels the message as **not** a problem:

```
Department: HR
Problem: no

You receive 18 days of paid annual leave per year, counted from the first day of the Nepali fiscal year.
```

**5. The response.** The server removes the `Department` and `Problem` lines. `Problem: no`, so no ticket is offered:

```json
{
  "reply": {
    "answer": "You receive 18 days of paid annual leave per year, counted from the first day of the Nepali fiscal year.",
    "department": "HR"
  }
}
```

`department` is the department of the section used. The page does not need it for the text, but it is part of the response.

**A question the knowledge does not answer** (kind 2). If the employee asks `Does the office pay for my home internet?`, and no section is about it, the search finds nothing above the threshold. The prompt tells the model never to guess in that case. It says that it has no such information, and who to contact:

```
Department: HR

I don't have information about that. Please contact HR at hr@ticket.com.
```

```json
{ "reply": { "answer": "I don't have information about that. Please contact HR at hr@ticket.com.", "department": "HR" } }
```

The contact comes from the `Departments` table (the department's description and contact are filled into the prompt), not from the code. A question that is not about work (a poem, a joke) gets a polite refusal with `Department: none` (kind 4).

## 8. An issue: "I am not able to reset my password"

The employee now reports a problem. The request is the same kind of request, with the conversation so far in `history`:

```http
POST /api/chat/stream
Authorization: Bearer <employee token>

{
  "message": "i am not able to reset my password",
  "history": [
    { "role": "user", "content": "hello" },
    { "role": "assistant", "content": "Hello! How can I help you today?" },
    { "role": "user", "content": "How many leave days do I get?" },
    { "role": "assistant", "content": "You receive 18 days of paid annual leave per year, ..." }
  ]
}
```

**1. Search and filter.** The message has 8 words, so it is searched as it is. The closest sections are about passwords:

| Section | Department | Score |
| --- | --- | --- |
| Password and MFA - Resetting your password | IT | 0.81 |
| Password and MFA - Multi-factor authentication | IT | 0.78 |
| Accounts - Locked account | IT | 0.52 |

The last one is more than 0.05 below the best, so it is dropped. Two sections go into the prompt, and the model is told to answer **only** from them.

**2. The model's reply** (kind 1, and this time the employee reports something that does not work for them, so `Problem: yes`):

```
Department: IT
Problem: yes

You can try resetting your password at password.ticket.com. If you cannot reach the multi-factor prompt, contact IT support.
```

**3. What the server adds.** The model only **labels** the message as a problem. It does not write the ticket offer: the server adds it, so the sentence is always the same and the model cannot add it by habit to an ordinary question. It does this when `Problem: yes`, the caller is an employee, and the department is a known one:

```json
{
  "reply": {
    "answer": "You can try resetting your password at password.ticket.com. If you cannot reach the multi-factor prompt, contact IT support.\n\nIf this does not solve the issue, I can create a ticket for the IT department. Let me know.",
    "department": "IT"
  }
}
```

Nothing is created yet. The employee has a choice: if the answer solved the problem, that is the end. If not, the conversation goes on, and the ticket is created in the next section.

## 9. The AI creates a ticket

The model never creates a ticket. It writes lines, and the **server** acts on them. This keeps a wrong output from creating anything on its own: a ticket needs the exact `Action: create_ticket` line, a title, a known department and a logged-in employee.

### 9.1 The conversation

**1. The employee describes a problem.** This is section 8: the model labels the message `Problem: yes`, and the server adds the ticket offer to the answer. The employee sees: `If this does not solve the issue, I can create a ticket for the IT department. Let me know.`

**2. "that did not work"** (kind 5). The model gets the last message only (its own answer from step 1). The whole conversation the page sent is still used for the search: the message has 4 words, so the search query is `"i am not able to reset my password that did not work"`.

```
Department: IT

Resetting your password did not work. Do you want me to create a ticket for the IT department?
```

**3. "yes"** (kind 6). The model gets only its previous message, the offer, which names the problem and the department. It sees that it offered a ticket, and replies:

```
Department: IT
Action: create_ticket
Title: Cannot reset password
Description: The employee cannot reset their password with the self-service page, and the suggested steps did not work.

I'm creating the ticket now.
```

The ticket offer in step 2 names the problem and the department, so the title and the description of the ticket can be taken from that one message when the employee says "yes". That is why `chat.history_messages` can be set as low as 1 for a smaller prompt and a faster reply. With the default of 3 the model sees a little more of the conversation as well.

### 9.2 What the server does

1. Removes the `Department:` line, then the three ticket lines.
2. Creates a ticket only if `Action` is `create_ticket` **and** the title is not empty. A missing description falls back to the title; the title is cut at 200 characters and the description at 4000.
3. Replies with a plain message, and creates nothing, when: the caller is the admin (the admin is not an employee), the department line is not a known department, or the department has no members.
4. Picks the assignee **round-robin** within the department: members in order of id, excluding anyone with the admin's username and the reporter (unless they are the only member), continuing after the assignee of the department's latest ticket and wrapping around at the end. A static lock covers choosing the person and saving the ticket.
5. Saves the ticket (`Status = New`, reporter from the token) and puts the facts after the model's sentence:

```json
{
  "answer": "I'm creating the ticket now.\n\nTicket #1 was created and assigned to Aarav Sharma (IT).",
  "department": "IT"
}
```

The model does not know who the ticket goes to, which is why the prompt tells it not to name the assignee.

### 9.3 The requests and responses, in simple words

There is **no "create ticket" request** that the page sends. The page only sends the chat message, as it does for every message. The ticket is created inside the server while it handles that message. Here is what goes over the wire for the same conversation.

**Who is asking.** Every request carries the login token in a header, `Authorization: Bearer <token>`. The server reads the employee id from it, and that employee becomes the **reporter** of the ticket. The page never sends who the reporter is.

**Request 1: the problem.** `POST /api/chat/stream`. The body is the new message and the earlier messages of the conversation (the server keeps no conversation of its own):

```json
{
  "message": "i am not able to reset my password",
  "history": []
}
```

The answer comes back one JSON object per line, so the page can show the text while it is written. `draft` lines are the reply so far, and the last line, `reply`, is the final answer:

```
{"draft":"You can try resetting your password at password.ticket.com ..."}
{"reply":{"answer":"You can try resetting your password at password.ticket.com ...\n\nIf this does not solve the issue, I can create a ticket for the IT department. Let me know.","department":"IT"}}
```

**Request 2: the problem is not solved.** The page sends the whole conversation so far in `history`:

```json
{
  "message": "that did not work",
  "history": [
    { "role": "user", "content": "i am not able to reset my password" },
    { "role": "assistant", "content": "You can try resetting your password ... Let me know." }
  ]
}
```

```json
{
  "reply": {
    "answer": "Resetting your password did not work. Do you want me to create a ticket for the IT department?",
    "department": "IT"
  }
}
```

Nothing is created yet. The model only offered.

**Request 3: "yes".** This is the request that creates the ticket. The body looks like the one before, with one more pair of messages in `history`:

```json
{
  "message": "yes",
  "history": [
    { "role": "user", "content": "i am not able to reset my password" },
    { "role": "assistant", "content": "You can try resetting your password ... Let me know." },
    { "role": "user", "content": "that did not work" },
    {
      "role": "assistant",
      "content": "Resetting your password did not work. Do you want me to create a ticket for the IT department?"
    }
  ]
}
```

Inside the server, in this order:

1. The model sees its own offer and writes the ticket lines (`Action: create_ticket`, `Title: ...`, `Description: ...`) in its raw reply (section 9.1, step 3).
2. The server checks the lines (section 9.2), picks the person in the IT department in turn, and saves the row in the `Tickets` table:

   | Column                 | Value                                        |
   | ---------------------- | -------------------------------------------- |
   | `Title`, `Description` | from the model's lines                       |
   | `Department`           | IT, the one the model named                  |
   | `ReportedBy`           | the employee from the token                  |
   | `AssignedTo`           | the next IT member in turn (never the admin) |
   | `Status`               | `New`                                        |
   | `CreatedAt`            | now                                          |

3. It answers with the model's sentence, and its own facts after it:

```json
{
  "reply": {
    "answer": "I'm creating the ticket now.\n\nTicket #1 was created and assigned to Aarav Sharma (IT).",
    "department": "IT"
  }
}
```

If the line says `create_ticket` but something is wrong (the department is unknown, it has no members, the caller is the admin, the title is empty), no ticket is saved, and the answer is a plain message instead.

**Seeing the ticket: `GET /api/tickets`.** No body. The Tickets page asks for the list, and the server returns the tickets the person reported or that are assigned to them (the admin gets all of them):

```json
[
  {
    "id": 1,
    "title": "Cannot reset password",
    "description": "The employee cannot reset their password with the self-service page, and the suggested steps did not work.",
    "department": "IT",
    "status": "New",
    "reportedBy": "Meera Thapa",
    "reportedByUsername": "meera",
    "assignedTo": "Aarav Sharma",
    "assignedToUsername": "aarav",
    "createdAt": "2026-11-01T10:42:00Z"
  }
]
```

**Changing the status: `PUT /api/tickets/1/status`.** Only the person the ticket is assigned to can do it. The body holds the new status, which is `New`, `InProgress` or `Completed`:

```json
{ "status": "InProgress" }
```

The response is the changed ticket, in the same shape as above. For anyone else the server answers `403` with a message, and `404` when the ticket does not exist.

## 10. Retrieval: finding the right sections

This is the first half of every chat message. The request from the client:

```http
POST /api/chat
Authorization: Bearer <employee token>

{ "message": "i am not able to reset my password", "history": [] }
```

`history` is up to 40 earlier `{ "role": "user" | "assistant", "content": "..." }` items. The server keeps no conversation; the page sends it each time.

### 10.1 Build the search query

Short follow-ups only make sense with the question before them ("and casual leave?"). If the message has `retrieval.follow_up_max_words` words or fewer (5 by default), the query is `"{previous user message} {message}"`. Otherwise the query is the message alone. This is a rule on the word count, not a list of phrases.

### 10.2 Embed the query

```http
POST {embedding.base_url}/api/embed
{ "model": "nomic-embed-text", "input": ["search_query: i am not able to reset my password"] }
```

### 10.3 Find the closest sections

Raw SQL, because EF Core cannot express the function. The sqlite-vec extension is loaded into each connection by `SqliteVecConnectionInterceptor`:

```sql
SELECT Id, DocumentTitle, Category, Heading, Content, SourceFile,
       vec_distance_cosine(Embedding, @queryVector) AS Distance
FROM KnowledgeChunks
ORDER BY Distance
LIMIT 5;                         -- retrieval.top_k
```

This compares the query with every row. For a few thousand sections that is fast. The extension is what makes it a single SQL query instead of a loop in C#.

### 10.4 Filter

`score = 1 - Distance`. Sections below `retrieval.min_score` (0.5) are dropped, and so are sections scoring more than `retrieval.max_score_gap` (0.05) below the best one. Whatever remains, possibly nothing, goes into the prompt.

### 10.5 Test retrieval on its own

```http
GET /api/knowledge/search?q=how many leave days do I get&k=3        (admin only)
```

```json
[
  {
    "id": 14,
    "documentTitle": "Leave Policy",
    "category": "HR",
    "heading": "Annual leave entitlement",
    "content": "Full-time employees receive 18 days...",
    "sourceFile": "leave-policy.md",
    "score": 0.78
  }
]
```

## 11. Generation: the prompt and the model

### 11.1 How the prompt is built

`ChatPromptBuilder` creates the list of messages sent to the model:

1. One `system` message: the `chat.system_prompt` setting, with the department placeholders filled in.
2. The history, trimmed to the last `chat.history_messages` messages (3 by default).
3. **One final `user` message** that contains the sections, the employee's message and the format reminder:

```
Sections:

[Password and MFA - Resetting your password | IT]
Use the self-service reset page at password.ticket.com ... If you cannot reach the multi-factor prompt, contact IT support.

[Password and MFA - Multi-factor authentication | IT]
...

Message: i am not able to reset my password

(Begin your reply with the Department line. ... Add the Action, Title and Description lines only if the employee just agreed to a ticket you offered.)
```

- The text of each section is the stored `Content`. The label `[title - heading | department]` is added by the server.
- If nothing passed the filter, the message is `No company documents matched this message.` instead of the sections.
- The reminder is repeated at the end because a small model pays the most attention to the end of the message.
- The **departments** (name, what it handles, contact) come from the `Departments` table and replace `{{departments}}` and `{{department_names}}` in the system prompt and in the reminder. No department name is written in code.

### 11.2 The request to the model

```http
POST {ollama.base_url}/api/chat
{
  "model": "qwen2.5:7b",
  "stream": true,
  "options": { "num_ctx": 4096, "temperature": 0, "top_p": 0.9, "num_predict": 500 },
  "messages": [
    { "role": "system", "content": "You are the Ticketing AI assistant ... (chat.system_prompt)" },
    { "role": "user", "content": "Sections:\n\n[Password and MFA - Resetting your password | IT]\n..." }
  ]
}
```

```json
{
  "model": "qwen2.5:7b",
  "message": {
    "role": "assistant",
    "content": "Department: IT\nProblem: yes\n\nYou can try resetting your password at password.ticket.com ..."
  },
  "done": true
}
```

`temperature` 0 makes the answers as repeatable as possible. `stream` is true: Ollama sends the reply in pieces, and the server passes them on to the page as the `draft` lines (section 6). With an online service the same messages go to `POST {chat.base_url}/chat/completions` with `Authorization: Bearer <key>`.

### 11.3 The six kinds of message

Every message is handled by the same prompt. The model decides which kind it is, and each kind says which `Department:` line to use. No code branches on the kind, and there are no word lists: the prompt decides.

| #   | Kind                                       | What the model must do                                                                                                                                                                                                                                                                |
| --- | ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | A question the sections answer             | Answer only from the sections, with the exact numbers, names and addresses. The reply also has a `Problem: yes` or `Problem: no` line: yes only when the employee reports something broken or blocked, no for a question about a rule, amount or date. Department of the section used |
| 2   | A work question the sections do not answer | Say it has no such information, and give the contact of the right department. Never guess, never say the company does or does not offer something                                                                                                                                     |
| 3   | Casual chat (greeting, thanks, small talk) | A short friendly reply in different words each time. `Department: none`                                                                                                                                                                                                               |
| 4   | An unrelated task (poems, trivia, code)    | Decline politely. `Department: none`                                                                                                                                                                                                                                                  |
| 5   | The problem is not solved                  | Offer to create a ticket and ask for confirmation. Create nothing                                                                                                                                                                                                                     |
| 6   | The employee agreed to the offered ticket  | Reply with the ticket lines (section 9)                                                                                                                                                                                                                                              |

## 12. Turning the reply into a response

The model answers in text. The server parses it (`ChatService`):

1. **Department line.** The first line `Department: <name>` is taken off (it is also accepted at the end of the reply). If the name matches a row in `Departments` (ignoring case) it becomes `department`, otherwise `null`. So a made-up department can never be returned.
2. **Problem and ticket lines.** `Problem:`, `Action:`, `Title:` and `Description:` lines at the start of what remains are always taken off, so they are never shown to the employee. If the `Problem` line says `yes` (and the caller is an employee and the department is known), the **server** adds the ticket offer sentence at the end of the answer. The model only labels the message; it does not write the offer, so it cannot add it by habit to an ordinary question. See section 11.
3. The rest is the answer.

```json
{
  "answer": "You can try resetting your password at password.ticket.com ...\n\nIf this does not solve the issue, I can create a ticket for the IT department. Let me know.",
  "department": "IT"
}
```

`department` is `null` for casual chat and unrelated requests. The page shows only `answer`. If Ollama is unreachable or the model is missing, the response is `503` with a problem-details `detail` message.

## 13. Concepts used in this app

**Chunk.** One `## ` section of an uploaded file is one chunk, and one row in `KnowledgeChunks`. A chunk is the unit that gets retrieved, so a section must make sense on its own: name the topic in the text and do not rely on the heading above it.

**Embedding and similarity.** A vector is just a list of numbers. The question and each chunk become vectors, and the closest vectors are the most related texts. "Closest" is measured as **cosine distance**; the app uses `score = 1 - distance`, so 1 is identical and lower is less related.

**Prefixes.** `nomic-embed-text` was trained with a label in front of each text. The app writes `search_document:` in front of every chunk when it is embedded, and `search_query:` in front of the user's message. This puts a question close to the passage that answers it. The prefix is only used to make the vector: the text saved in the table and the text shown to the chat model have no prefix. The same two prefixes are used for every embedding model (they are fixed in `Knowledge/EmbeddingText.cs`, not settings), because a model that does not need them is not hurt by them.

**What similarity cannot do.** In our test questions the score did **not** separate answerable from unanswerable questions: answerable questions scored as low as 0.65, while questions the documents do not cover scored as high as 0.79. So a threshold cannot decide "do we know this?". The app uses `retrieval.min_score` (0.6) only to drop clearly unrelated sections, and the **prompt** tells the model what to do when the sections do not answer the question.

**Measured during development** (Intel i9 Mac, 32 GB, CPU only, 37 sample documents with about 270 sections, about 83 test questions):

- The right document was in the top 5 for 99% of the questions and was the top result for 92%.
- Embedding all 270 sections took about 38 seconds.
- Qwen 7B read about 43 tokens per second and wrote about 6 tokens per second, so a company question took roughly 15 to 25 seconds and casual chat 5 to 8.

## 14. Settings that tune the AI

All of these are rows in the `Settings` table, editable on the Settings page (admin). They are read before every embedding and chat call with no cache, so a change applies to the next message.

| Key                             | Default                  | Effect                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------------------------- | ------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `embedding.base_url`            | `http://localhost:11434` | Where the embedding model runs. No path (`http://localhost:11434`) is Ollama's own API (`/api/embed`); a path (for example Gemini's `.../v1beta/openai`) is an online service in the OpenAI format (`/embeddings`). From a Docker container, an Ollama on the computer is `http://host.docker.internal:11434`                                                                                                                                                         |
| `embedding.api_key_name`        | (empty)                  | The name of the environment variable that holds the key of an online embedding service (for example `GEMINI_API_KEY`). The key itself is never stored                                                                                                                                                                                                                                                                                                                 |
| `embedding.model`               | `nomic-embed-text`       | The model that makes the vectors. **Changing it converts all the stored knowledge**: every section is embedded again with the new model, and the change is saved only when that worked. The embedding settings are changed with Use on the Settings page, not by editing them                                                                                                                                                                                         |
| `retrieval.top_k`               | 4                        | How many sections to fetch                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `retrieval.min_score`           | 0.5                      | Sections below this are dropped                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `retrieval.max_score_gap`       | 0.05                     | Sections scoring more than this below the best one are dropped too, so the prompt is shorter and the reply faster. `0` turns it off                                                                                                                                                                                                                                                                                                                                   |
| `retrieval.follow_up_max_words` | 5                        | Messages this short are searched together with the previous user message. `0` turns it off                                                                                                                                                                                                                                                                                                                                                                            |
| `chat.api_key_name`             | (empty)                  | The name of the environment variable that holds the key of an online chat service (for example `GROQ_API_KEY`). Empty for Ollama. The key itself is never stored                                                                                                                                                                                                                                                                                                      |
| `chat.base_url`                 | `http://localhost:11434` | Where the chat model is called, and this one setting decides how. An address with no path (`http://localhost:11434`, `http://host.docker.internal:11434` from a Docker container) is Ollama's own API. An address with a path (for example Gemini's `.../v1beta/openai`) is an online service in the OpenAI chat format. The key for an online service is the environment variable named in the key name of the model (for example `GEMINI_API_KEY`), never a setting |
| `chat.model`                    | `qwen2.5:7b`             | Model that writes the answers                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| `chat.temperature`              | 0.2                      | Randomness. 0 gives the most repeatable answers                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `chat.top_p`                    | 0.9                      | Sent to Ollama                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `chat.num_ctx`                  | 4096                     | Context window in tokens. The system prompt, history, sections and answer must fit                                                                                                                                                                                                                                                                                                                                                                                    |
| `chat.reasoning_effort`         | (empty)                  | Only for an online service: asks a thinking model to think less (for example `none`, `minimal`, `low`), which makes the first word arrive sooner. Empty sends nothing                                                                                                                                                                                                                                                                                                 |
| `chat.max_tokens`               | 2000                     | Maximum answer length (`num_predict` for Ollama, `max_tokens` for an online service). Replies are short because the prompt asks for that, so this is only a safety limit. Keep it generous: a model that thinks counts its thinking in it, and when it is used up the chat reports that the reply was cut off (raise it, or lower `chat.reasoning_effort`)                                                                                                            |
| `chat.history_messages`         | 3                        | How many recent messages the model gets. More messages mean a longer prompt and a slower reply. 1 is enough for a ticket, because the assistant's offer states the problem. A short follow-up is searched together with the previous question, taken from the whole conversation the page sends, whatever this is set to                                                                                                                                              |
| `chat.system_prompt`            | (long text)              | The six kinds of message and the reply format. Placeholders: `{{departments}}`, `{{department_names}}`                                                                                                                                                                                                                                                                                                                                                                |
| `chat.format_reminder`          | (short text)             | Repeated at the end of the final user message. Same placeholders                                                                                                                                                                                                                                                                                                                                                                                                      |

A model named in a setting must exist at its address: for Ollama it must be pulled (`ollama pull <model>` on that computer), and an online service has its own model names. Nothing writes the settings at startup: a setting with no row uses its default, and **Load default AI assistant data** (Settings, Manage App Data tab) saves all of them as rows (and resets any value we changed). So an old prompt stays until we edit it or restore the defaults.

**Making replies faster** (the time is the model reading the prompt, plus writing the answer; a hosted model may also think first): we lower `retrieval.top_k` and raise `retrieval.max_score_gap` carefully, so fewer sections are sent, lower `chat.history_messages` (1 is enough for a ticket) and `chat.max_tokens`, ask in the prompt for short answers, set `chat.reasoning_effort` for a thinking model on an API, or use a smaller or faster model. The server writes a timing line for every message (search time, time to the first text, total), which shows which step to work on.

## 15. Debugging a wrong answer

We work through it in this order. Each step rules out one part of the pipeline.

1. **Is the right section retrieved?** We call `GET /api/knowledge/search?q=<the question>` as the admin. If the right section is missing or has a low score, the problem is retrieval: the section is badly split or does not name its topic, the file was not uploaded, or the prefixes do not match the model. We fix the knowledge, not the prompt.
2. **Is it in the prompt?** If it was retrieved but the answer is wrong, we check `retrieval.min_score` (it may be dropping the section) and `retrieval.top_k`. A long section also competes for the context window (`chat.num_ctx`).
3. **Is the department right?** The `Department:` line is chosen by the model from the department descriptions in the `Departments` table. We improve the description of the department.
4. **Only then do we change the prompt.** The 7B model reacts strongly to small wording changes, so we re-test after any edit.

## 16. What we learned, and the limits

- **We describe the tone and do not paste example sentences.** When an example sentence was in the prompt, the model repeated it word for word. The one place that does contain a fixed sentence is the ticket offer, on purpose, because the wording is wanted exactly.
- **Rules per kind work better than one long rule.** One long rule sentence was ignored, while a rule for each kind of message was followed.
- **We put the format reminder last.** The model follows instructions near the end of the message best.
- **Temperature 0** gives the most repeatable answers. It does not make a 7B model deterministic across prompt changes.
- **Measured weaknesses.** The department line was right for all 14 test questions, but the **wording of "not covered" answers is unreliable**: it sometimes asserts "we do not offer X", adds advice or another contact, or stretches a nearby section. It can still write a poem when asked in a tricky way. These come from the model size and are the first things to re-test after a prompt change.
- **No server-side memory.** The conversation lives in the page and is sent with each message, so it is lost on logout or reload.
- **Changing the embedding model converts the store.** Every section is embedded again from its stored text, with the same prefix, and the old vectors are replaced together with the settings in one transaction. A failure changes nothing. It takes about a minute for 270 sections on a CPU.
- **Small sections matter.** A section that mixes two topics gets one vector that sits between them and matches neither well.

## 17. Where to look in the code

```
server/src/TicketingAI.Api/
  Knowledge/
    KnowledgeController.cs          upload, delete, search endpoints
    KnowledgeUploadParser.cs        the file format and its validation
    KnowledgeService.cs             embeds sections, stores them, searches
    OllamaEmbeddingClient.cs        POST /api/embed
    KnowledgeRepository.cs          the vec_distance_cosine query
  Chat/
    ChatService.cs                  the answer pipeline, parsing, ticket lines
    ChatPromptBuilder.cs            the message list sent to the model
    OllamaChatClient.cs             POST /api/chat
  Tickets/TicketService.cs          round-robin assignment
  Settings/                         SettingsService, SettingRules, restore defaults
server/src/TicketingAI.Data/
  Persistence/SettingDefaults.cs    the default prompt, reminder and every setting
  Persistence/SqliteVecConnectionInterceptor.cs   loads sqlite-vec into each connection
  Entities/KnowledgeChunk.cs
```
