# Developer docs: AI and RAG

How the assistant works and why it is built this way. It starts with the history (fine-tuning, and why we moved away from it), explains RAG and the vector database in plain words, and then follows the workflow of the app step by step. Every payload in the workflow sections is what the code sends or receives. Similarity scores, ids and the model's wording are **illustrative**, because the wording varies from run to run.

## 1. Where we started: fine-tuning

Our first version used a **fine-tuned model**. Fine-tuning means taking an existing AI model and training it a bit more on our own examples, so that it learns a task or our data.

We did this on Google Colab (a notebook with a free or cheap GPU). The model's job was to read an issue and tell us **which user and which team it should be assigned to**. The process looked like this:

1. Collect examples: past issues, with the team and the person that handled each one.
2. Turn them into a training file in the format the training code expects.
3. Open the Colab notebook, load a base model and run the training. This needs a GPU and takes a while.
4. Save the new model, download or upload it somewhere, and point the application at it.
5. Test it, and repeat from step 1 if the answers are not good enough.

The full write-up of this fine-tuning work, with the details of how we trained the model, is here: [Fine-tuning an AI model](https://www.susanpant.com.np/blogs/finetune-ai-model).

It worked for the job it was trained on. Then we needed to add **other company data** (policies, processes, who to contact), so that the assistant could answer questions and not only route issues. The model knew nothing about that data, so it needed **fine-tuning again**: prepare new data, run Colab again, test, redeploy.

That is the problem. With fine-tuning, the company's knowledge is baked **inside the model**:

- Every change to the data (a new policy, a changed contact, a removed rule) means preparing data and training again.
- Training needs a GPU, time, and someone who knows the notebook.
- We cannot simply edit or delete one fact. We train a new model and hope it learned the change and did not forget something else.
- It is hard to tell where an answer came from.

We did not want to train a model every time a document changes.

## 2. Our answer: RAG

Instead of retraining the model every time the company data changes, what if we could just **upload the company's data to a database**, and the AI could just **read the data in that database and reply to us**?

**This is RAG.**

The idea is simple: **do not teach the model our data. Give it the relevant data every time it answers.**

The model stays as it is. The company knowledge lives in a database. When someone asks a question, the app looks up the few pieces of knowledge that are related to the question, and hands them to the model together with the question: "answer this, using only these texts". This is called the **RAG pipeline**. It has two parts: one that **loads** the knowledge into the database (once per file), and one that **answers** a question (every chat message).

**Part 1: getting the knowledge ready** (done once for each file)

1. **We take the company documents.** For example the leave policy or the VPN guide. *(Data)*
2. **We read them.** The app opens the file and finds the title, the department and the sections. *(Parsing)*
3. **We cut them into small pieces.** One piece is about one topic. *(Chunking)*
4. **We turn each piece into numbers.** An AI model reads the piece and gives back a list of numbers that stands for its meaning. *(Embedding)*
5. **We save the pieces and their numbers in the database.** The text and its numbers are stored together. *(Vector DB)*

**Part 2: answering a question** (every time someone chats)

1. **The employee asks a question.** *(User query)*
2. **We turn the question into numbers**, with the same AI model, so it can be compared with the saved pieces. *(Embedding)*
3. **We find the pieces that are closest.** Pieces whose numbers are close to the numbers of the question have a close meaning, so they are the most related to the question. *(Semantic search)*
4. **We take those pieces out of the database.** This text is what the AI will read to answer. *(Retrieve context)*
5. **We give the pieces and the question to the chat AI**, and tell it to answer only from those pieces. *(LLM)*
6. **We show the answer to the employee.** *(Answer)*

Part 1 fills the database, and Part 2 searches it. Here are the same steps again, with an example.

**Loading the knowledge (once per file, done by an admin in the app)**

**Step 1. We write the company knowledge as documents.** One file per topic (for example the leave policy or the VPN guide), with the department it belongs to. Here is a short one:

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement
Full-time employees receive 18 days of paid annual leave per year. Leave is counted from the first day of the Nepali fiscal year.

## Sick leave
Employees can take up to 12 days of paid sick leave per year. A medical certificate is needed for more than 2 days in a row.
```

**Step 2. We split each document into small sections.** We split at every `## ` heading, so each section is one topic with its own title. The file above becomes two sections:

| Section | Title (heading) | Text (content) |
| --- | --- | --- |
| 1 | Annual leave entitlement | Full-time employees receive 18 days of paid annual leave per year. Leave is counted from the first day of the Nepali fiscal year. |
| 2 | Sick leave | Employees can take up to 12 days of paid sick leave per year. A medical certificate is needed for more than 2 days in a row. |

Small sections matter because each one gets its own entry in the database, and later the app finds and uses only the sections that match the question. One section about one topic is found much more reliably than a whole document about many topics.

**Step 3. We run an embedding model and send it the text of each section.** An embedding model is an AI model with one job: it turns a piece of text into a **list of numbers** (we call it a vector) that captures the meaning of the text. We run one, `nomic-embed-text`, with Ollama. Ollama starts a small web server on our machine (`http://localhost:11434`), and the model is used by sending it an API request.

For each section we send its text (with the document title, so the section keeps its context) as an API request:

```http
POST http://localhost:11434/api/embed
Content-Type: application/json

{
  "model": "nomic-embed-text",
  "input": [
    "search_document: Leave Policy - Annual leave entitlement\nFull-time employees receive 18 days of paid annual leave per year. Leave is counted from ...",
    "search_document: Leave Policy - Sick leave\nEmployees can take up to 12 days of paid sick leave per year. A medical certificate is ..."
  ]
}
```

The model answers with one list of numbers for each text we sent, in the same order:

```json
{
  "embeddings": [
    [0.0123, -0.0456, 0.1012, -0.0087, ... ],
    [0.0311,  0.0052, -0.0874,  0.0459, ... ]
  ]
}
```

Each list has 768 numbers (the examples are cut short). Two texts with a similar meaning get lists that are close to each other, and texts about different things get lists that are far apart. That is what makes the search in step 6 possible. (The `search_document:` at the start is a label this model expects; it is explained under Prefixes in section 6.)

**Step 4. We save each section in the database: its text, and next to it its list of numbers.** One row per section:

| Heading | Content (the text) | Embedding (the list of numbers) |
| --- | --- | --- |
| Annual leave entitlement | Full-time employees receive 18 days ... | `[0.0123, -0.0456, 0.1012, ...]` |
| Sick leave | Employees can take up to 12 days ... | `[0.0311, 0.0052, -0.0874, ...]` |

The readable text is saved too, because that text is what we later give to the chat model. The numbers are only used to find the right rows.

**Answering a question (every chat message)**

5. We convert the question into a list of numbers with the same embedding model.
6. We ask the database for the sections whose numbers are **closest** to the numbers of the question. Closest numbers mean closest meaning.
7. We put those sections and the question into one prompt, and ask the chat model to answer using only that text.
8. We show the answer. If the problem is not solved, the assistant offers to create a ticket.

The result: **to change what the assistant knows, upload a file.** To remove knowledge, delete it. There is no training, no Colab and no waiting.

| | Fine-tuning | RAG (what we use now) |
| --- | --- | --- |
| Where the company knowledge lives | Inside the model | In a database, as text |
| Adding or changing knowledge | Prepare data, train again, redeploy | Upload a file |
| Removing knowledge | Train again | Delete the rows |
| Needs a GPU and training | Yes | No |
| Can show where an answer came from | No | Yes: the sections that were used are known |
| Cost of a change | Hours | Minutes |

### How to implement RAG in a new project

We need four things: two models, knowledge files, one table, and a little code for each of the two pipelines. The reference for each step is a file in this repository.

**1. We get the two models.** An embedding model that makes the lists of numbers, and a chat model that writes the answers. We run both with [Ollama](https://ollama.com):

```bash
ollama pull nomic-embed-text      # embeddings: 768 numbers per text
ollama pull qwen2.5:7b            # answers
```

**2. We write the knowledge files.** One file per topic, a title and a department at the top, then sections. Each section becomes one row, so it must make sense on its own and name its topic.

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement
Full-time employees receive 18 days of paid annual leave per year...
```

- Reference: the 37 sample files in `server/src/TicketingAI.Api/Knowledge/Documents` (HR, IT and Finance, set in Nepal), and the format notes and about 83 test questions in `server/src/TicketingAI.Api/Knowledge/README.md`.
- We start with our own 5 to 10 files. Test questions come later, in step 7.

**3. We create the table `KnowledgeChunks`.** A normal table, with one extra column, `Embedding`, that holds the list of numbers as raw float32 values (a BLOB). In SQL:

```sql
CREATE TABLE KnowledgeChunks (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    DocumentTitle TEXT NOT NULL,       -- title from the file
    Category      TEXT NOT NULL,       -- the department the file belongs to
    Heading       TEXT NOT NULL,       -- the section title
    Content       TEXT NOT NULL,       -- the section text (what the chat model reads)
    SourceFile    TEXT NOT NULL,       -- the file name
    ContentHash   TEXT NOT NULL,       -- SHA-256 of Content
    Embedding     BLOB NOT NULL,       -- the list of numbers, float32 values
    IngestedAt    TEXT NOT NULL
);
CREATE UNIQUE INDEX IX_KnowledgeChunks_SourceFile_Heading ON KnowledgeChunks (SourceFile, Heading);
CREATE INDEX IX_KnowledgeChunks_Category ON KnowledgeChunks (Category);
```

- Reference: `server/src/TicketingAI.Data/Entities/KnowledgeChunk.cs` and `Persistence/Configurations/KnowledgeChunkConfiguration.cs`. We create the table with an EF Core migration instead of hand-written SQL.
- We read the `Embedding` column only when we search. Listing screens should not select it.

**4. We turn on the vector functions.** We install the **sqlite-vec** extension (in .NET the NuGet package `sqlite-vec`; in Python `pip install sqlite-vec`) and load it into every connection. We check that it works:

```sql
SELECT vec_version();
```

- Reference: `server/src/TicketingAI.Data/Persistence/SqliteVecConnectionInterceptor.cs` loads it into each connection EF Core opens, and `Program.cs` checks `vec_version()` at startup so a broken install fails loudly.

**5. We load the knowledge (the ingest code).** For each section of each file we do this:

```text
text   = "search_document: " + title + " - " + heading + "\n" + content
vector = embed(text)                         -- POST /api/embed on Ollama, returns 768 numbers
INSERT INTO KnowledgeChunks (..., Content, Embedding) VALUES (..., content, float32_bytes(vector))
```

- We save the plain `content` (without the prefix) as the text, and the vector in `Embedding`.
- We embed all sections before saving any, so a failed run does not leave half the data.
- Reference: `Knowledge/KnowledgeUploadParser.cs` (splits a file into sections), `Knowledge/OllamaEmbeddingClient.cs` (the embed call), `Knowledge/KnowledgeService.cs` (builds the rows and saves them). The prefix is `search_document:` for `nomic-embed-text`. Other models use other prefixes, or none.

**6. We answer a question (the query code).**

```text
vector = embed("search_query: " + question)       -- the same embedding model
rows   = SELECT Heading, Content, vec_distance_cosine(Embedding, vector) AS Distance
         FROM KnowledgeChunks ORDER BY Distance LIMIT 5
rows   = rows whose (1 - Distance) is at least 0.5
prompt = system prompt + the rows + the question + a format reminder
answer = chat(prompt)                              -- POST /api/chat on Ollama
```

- Reference: `Knowledge/KnowledgeRepository.cs` (the distance query), `Chat/ChatPromptBuilder.cs` (builds the prompt), `Chat/OllamaChatClient.cs` (the chat call), `Chat/ChatService.cs` (the whole pipeline), and the system prompt in `server/src/TicketingAI.Data/Persistence/SettingDefaults.cs`.
- The system prompt must tell the model to answer only from the sections and what to say when they do not contain the answer. The score threshold alone cannot decide that (section 6).

**7. We test it in two stages.** First we test retrieval without the model: for each test question, we check that the right section is among the top results. In this app that is `GET /api/knowledge/search?q=...`. Only when retrieval is good, we test the answers. If retrieval is wrong, we fix the files and sections, not the prompt.

**Rules to remember**
- We use the **same embedding model** for the files and for the questions. If we change it, we load all the files again.
- We use the **prefix** that the embedding model was trained with, on both sides.
- We keep the **settings** (model names, number of results, score threshold, prompt) in a table or a config file, not in code. We read them from the `Settings` table, so they can be changed without a deploy.
- We plan for the edit flow from the start: replacing or deleting the rows of a file must be one operation. Ours is the Add and Flush and Add upload.

## 3. What RAG is

**RAG** stands for **Retrieval-Augmented Generation**:

- **Retrieval:** find the company texts that are related to the question.
- **Augmented:** add those texts to the prompt, so the model has more to work with.
- **Generation:** the model writes the answer from them.

A good picture is an **open-book exam**. A fine-tuned model is a student who studied the book beforehand and answers from memory: it is fast, but if the book changes, the student must study again. With RAG, the student is allowed to open the book and read the right page before answering. The book can change at any time, and the next answer already uses the new page.

What RAG does **not** do: it does not change the model, and it does not make the model always right. The model can still misread the text it was given. And if the right section is not found, the model has nothing to answer from. This is why a lot of the work in this app is about finding the right sections and about telling the model what to do when there is no answer (see the workflow sections).

## 4. What a vector database is

Computers cannot compare the **meaning** of two texts directly. "I forgot my login" and "how do I reset my password" share almost no words, but they mean nearly the same. A normal text search (`LIKE '%password%'`) would miss the first one.

An **embedding model** solves this. It converts a text into a **list of numbers** (a vector). Texts with a similar meaning get similar numbers. Our embedding model, `nomic-embed-text`, produces 768 numbers for each text.

A **vector database** is, in simple terms, **a normal database table with one extra column that holds that list of numbers, plus a function that tells how close two lists are.** That is all. In our app it is the `KnowledgeChunks` table in SQLite:

| Id | Heading | Content (the text) | Embedding (the extra column: a list of numbers) |
| --- | --- | --- | --- |
| 14 | Annual leave entitlement | Full-time employees receive 18 days of paid annual leave per year... | `[0.012, -0.045, 0.101, ... 768 numbers]` |
| 15 | Sick leave | Employees can take up to 12 days of paid sick leave... | `[0.031, 0.005, -0.087, ... 768 numbers]` |
| 21 | Resetting your password | Use the self-service reset page at password.ticket.com... | `[-0.090, 0.064, 0.022, ... 768 numbers]` |

The extra feature is the **distance comparison**. We give the database the numbers of the question, and it returns the rows whose numbers are closest, with a score:

```sql
SELECT Heading, Content,
       vec_distance_cosine(Embedding, @numbersOfTheQuestion) AS Distance
FROM KnowledgeChunks
ORDER BY Distance          -- smallest distance = closest meaning
LIMIT 5;                   -- the 5 best matches
```

- `vec_distance_cosine` is the comparison function. Two lists pointing in nearly the same direction have a distance close to 0. The app turns it into a score, `1 - distance`: 1 is identical, lower is less related.
- The function comes from **sqlite-vec**, a small extension that is loaded into SQLite. That is why we do not run a separate vector database: it is the same SQLite file, the same table, and one extra column.
- It compares the question with every row. For the size of data here (hundreds to a few thousand sections) that is fast. A separate vector database only becomes necessary for much larger data.

Two rules keep the numbers meaningful:

- The question and the sections must be converted by the **same embedding model**. Numbers from two different models cannot be compared, so changing the embedding model converts all the stored knowledge: every section is embedded again from its text, and the new vectors replace the old ones together with the new settings.
- The numbers only represent meaning. The readable text is stored in the same row, and that text is what the chat model receives.

The next sections follow the whole workflow of the app in detail, with the real requests and responses.

## 5. The big picture

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

| Piece | What it is |
| --- | --- |
| Embedding model | `nomic-embed-text`. Turns a text into a list of 768 numbers (a **vector**). Texts with similar meaning get similar vectors |
| Chat model | `qwen2.5:7b`. Reads the prompt and writes the answer |
| Vector store | The `KnowledgeChunks` table in SQLite, with the **sqlite-vec** extension for the similarity query. There is no separate vector database |
| Ollama | Runs the models on our own computer, when we use local models. It is optional: the models can be online services, and Docker does not include it |

Both model names, and every number that tunes the behaviour, are rows in the `Settings` table (see [section 12](#12-settings-that-tune-the-ai)). The app reads them on every request.

## 6. Concepts used in this app

**Chunk.** One `## ` section of an uploaded file is one chunk, and one row in `KnowledgeChunks`. A chunk is the unit that gets retrieved, so a section must make sense on its own: name the topic in the text and do not rely on the heading above it.

**Embedding and similarity.** A vector is just a list of numbers. The question and each chunk become vectors, and the closest vectors are the most related texts. "Closest" is measured as **cosine distance**; the app uses `score = 1 - distance`, so 1 is identical and lower is less related.

**Prefixes.** `nomic-embed-text` was trained with a label in front of each text. The app writes `search_document:` in front of every chunk when it is embedded, and `search_query:` in front of the user's message. This puts a question close to the passage that answers it. The prefix is only used to make the vector: the text saved in the table and the text shown to the chat model have no prefix. The same two prefixes are used for every embedding model (they are fixed in `Knowledge/EmbeddingText.cs`, not settings), because a model that does not need them is not hurt by them.

**What similarity cannot do.** In our test questions the score did **not** separate answerable from unanswerable questions: answerable questions scored as low as 0.65, while questions the documents do not cover scored as high as 0.79. So a threshold cannot decide "do we know this?". The app uses `retrieval.min_score` (0.6) only to drop clearly unrelated sections, and the **prompt** tells the model what to do when the sections do not answer the question.

**Measured during development** (Intel i9 Mac, 32 GB, CPU only, 37 sample documents with about 270 sections, about 83 test questions):
- The right document was in the top 5 for 99% of the questions and was the top result for 92%.
- Embedding all 270 sections took about 38 seconds.
- Qwen 7B read about 43 tokens per second and wrote about 6 tokens per second, so a company question took roughly 15 to 25 seconds and casual chat 5 to 8.

## 7. Ingestion: from a file to a vector

An admin uploads one or more `.md` or `.txt` files for a department on the Knowledge page.

```http
POST /api/knowledge/upload            (admin only, multipart/form-data)

departmentId = 2
flush        = false
files        = leave-policy.md
```

`flush=false` is **Add**: a file with the same name replaces its earlier sections. `flush=true` is **Flush and Add**: all existing knowledge is deleted first (only if at least one file is valid).

### 3.1 The file format

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement
Full-time employees receive 18 days of paid annual leave per year...

## Sick leave
...
```

`title:` and `department:` come first; each `## ` line starts a section. A file is rejected, with a message per file, for a missing title or department, text before the first section, no sections, an empty or repeated section title, an empty section, being larger than 1 MB, or a department that is not the selected one (the file is then discarded).

### 3.2 Steps

1. **Parse and validate** (`KnowledgeUploadParser`).
2. **Build the text to embed** for each section: `"search_document: {title} - {heading}\n{content}"`:
   ```
   search_document: Leave Policy - Annual leave entitlement
   Full-time employees receive 18 days of paid annual leave per year...
   ```
   The title and heading give the section context, so a short section still carries its topic.
3. **Embed** in batches of 16:
   ```http
   POST {embedding.base_url}/api/embed
   { "model": "nomic-embed-text", "input": ["search_document: Leave Policy - Annual leave ...", "search_document: Leave Policy - Sick leave ..."] }
   ```
   ```json
   { "embeddings": [[0.0123, -0.0456, ...], [0.0311, 0.0052, ...]] }
   ```
4. **Store one row per section** in `KnowledgeChunks`:

   | Column | Value |
   | --- | --- |
   | `DocumentTitle`, `Heading`, `Content` | From the file. `Content` has no prefix |
   | `Category` | The **department name** (this is what ties a chunk to a department) |
   | `SourceFile` | The uploaded file name |
   | `ContentHash` | SHA-256 of the content |
   | `Embedding` | The 768 numbers as raw **float32** values in a blob (3072 bytes) |
   | `IngestedAt` | UTC time |

5. All files are validated and embedded **before** anything is saved, so if Ollama fails the existing knowledge stays as it was.

Removing a department's knowledge is `DELETE /api/knowledge/department/{id}`, which deletes the rows whose `Category` is that department's name.

## 8. Retrieval: finding the right sections

This is the first half of every chat message. The request from the client:

```http
POST /api/chat
Authorization: Bearer <employee token>

{ "message": "i am not able to reset my password", "history": [] }
```

`history` is up to 40 earlier `{ "role": "user" | "assistant", "content": "..." }` items. The server keeps no conversation; the page sends it each time.

### 4.1 Build the search query

Short follow-ups only make sense with the question before them ("and casual leave?"). If the message has `retrieval.follow_up_max_words` words or fewer (5 by default), the query is `"{previous user message} {message}"`. Otherwise the query is the message alone. This is a rule on the word count, not a list of phrases.

### 4.2 Embed the query

```http
POST {embedding.base_url}/api/embed
{ "model": "nomic-embed-text", "input": ["search_query: i am not able to reset my password"] }
```

### 4.3 Find the closest sections

Raw SQL, because EF Core cannot express the function. The sqlite-vec extension is loaded into each connection by `SqliteVecConnectionInterceptor`:

```sql
SELECT Id, DocumentTitle, Category, Heading, Content, SourceFile,
       vec_distance_cosine(Embedding, @queryVector) AS Distance
FROM KnowledgeChunks
ORDER BY Distance
LIMIT 5;                         -- retrieval.top_k
```

This compares the query with every row. For a few thousand sections that is fast. The extension is what makes it a single SQL query instead of a loop in C#.

### 4.4 Filter

`score = 1 - Distance`. Sections below `retrieval.min_score` (0.5) are dropped, and so are sections scoring more than `retrieval.max_score_gap` (0.05) below the best one. Whatever remains, possibly nothing, goes into the prompt.

### 4.5 Test retrieval on its own

```http
GET /api/knowledge/search?q=how many leave days do I get&k=3        (admin only)
```
```json
[ { "id": 14, "documentTitle": "Leave Policy", "category": "HR", "heading": "Annual leave entitlement",
    "content": "Full-time employees receive 18 days...", "sourceFile": "leave-policy.md", "score": 0.78 } ]
```

## 9. Generation: the prompt and the model

### 5.1 How the prompt is built

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

### 5.2 The request to the model

```http
POST {ollama.base_url}/api/chat
{
  "model": "qwen2.5:7b",
  "stream": false,
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
  "message": { "role": "assistant", "content": "Department: IT\nProblem: yes\n\nYou can try resetting your password at password.ticket.com ..." },
  "done": true
}
```

`temperature` 0 makes the answers as repeatable as possible. `stream` is false: the server waits for the whole reply and returns it in one response.

### 5.3 The six kinds of message

Every message is handled by the same prompt. The model decides which kind it is, and each kind says which `Department:` line to use. No code branches on the kind, and there are no word lists: the prompt decides.

| # | Kind | What the model must do |
| --- | --- | --- |
| 1 | A question the sections answer | Answer only from the sections, with the exact numbers, names and addresses. The reply also has a `Problem: yes` or `Problem: no` line: yes only when the employee reports something broken or blocked, no for a question about a rule, amount or date. Department of the section used |
| 2 | A work question the sections do not answer | Say it has no such information, and give the contact of the right department. Never guess, never say the company does or does not offer something |
| 3 | Casual chat (greeting, thanks, small talk) | A short friendly reply in different words each time. `Department: none` |
| 4 | An unrelated task (poems, trivia, code) | Decline politely. `Department: none` |
| 5 | The problem is not solved | Offer to create a ticket and ask for confirmation. Create nothing |
| 6 | The employee agreed to the offered ticket | Reply with the ticket lines (section 11) |

## 10. Turning the reply into a response

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

## 11. The AI creates a ticket

The model never creates a ticket. It writes lines, and the **server** acts on them. This keeps a wrong output from creating anything on its own: a ticket needs the exact `Action: create_ticket` line, a title, a known department and a logged-in employee.

### 7.1 A conversation

**1. The employee describes a problem.** The model's raw reply (kind 1) labels it as a problem:

```
Department: IT
Problem: yes

You can try resetting your password at password.ticket.com ...
```

The server removes the `Problem` line and, because it said `yes`, adds the ticket offer, so the employee sees:

```
You can try resetting your password at password.ticket.com ...

If this does not solve the issue, I can create a ticket for the IT department. Let me know.
```

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

### 7.2 What the server does

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

### 7.3 The requests and responses, in simple words

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
{ "reply": { "answer": "Resetting your password did not work. Do you want me to create a ticket for the IT department?", "department": "IT" } }
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
    { "role": "assistant", "content": "Resetting your password did not work. Do you want me to create a ticket for the IT department?" }
  ]
}
```

Inside the server, in this order:
1. The model sees its own offer and writes the ticket lines (`Action: create_ticket`, `Title: ...`, `Description: ...`) in its raw reply (section 11, step 3).
2. The server checks the lines (section 7.2), picks the person in the IT department in turn, and saves the row in the `Tickets` table:

   | Column | Value |
   | --- | --- |
   | `Title`, `Description` | from the model's lines |
   | `Department` | IT, the one the model named |
   | `ReportedBy` | the employee from the token |
   | `AssignedTo` | the next IT member in turn (never the admin) |
   | `Status` | `New` |
   | `CreatedAt` | now |

3. It answers with the model's sentence, and its own facts after it:

```json
{ "reply": { "answer": "I'm creating the ticket now.\n\nTicket #1 was created and assigned to Aarav Sharma (IT).", "department": "IT" } }
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

## 12. Settings that tune the AI

All of these are rows in the `Settings` table, editable on the Settings page (admin). They are read before every embedding and chat call with no cache, so a change applies to the next message.

| Key | Default | Effect |
| --- | --- | --- |
| `embedding.base_url` | `http://localhost:11434` | Where the embedding model runs. No path (`http://localhost:11434`) is Ollama's own API (`/api/embed`); a path (for example Gemini's `.../v1beta/openai`) is an online service in the OpenAI format (`/embeddings`). From a Docker container, an Ollama on the computer is `http://host.docker.internal:11434` |
| `embedding.api_key_name` | (empty) | The name of the environment variable that holds the key of an online embedding service (for example `GEMINI_API_KEY`). The key itself is never stored |
| `embedding.model` | `nomic-embed-text` | The model that makes the vectors. **Changing it converts all the stored knowledge**: every section is embedded again with the new model, and the change is saved only when that worked. The embedding settings are changed with Use on the Settings page, not by editing them |
| `retrieval.top_k` | 4 | How many sections to fetch |
| `retrieval.min_score` | 0.5 | Sections below this are dropped |
| `retrieval.max_score_gap` | 0.05 | Sections scoring more than this below the best one are dropped too, so the prompt is shorter and the reply faster. `0` turns it off |
| `retrieval.follow_up_max_words` | 5 | Messages this short are searched together with the previous user message. `0` turns it off |
| `chat.api_key_name` | (empty) | The name of the environment variable that holds the key of an online chat service (for example `GROQ_API_KEY`). Empty for Ollama. The key itself is never stored |
| `chat.base_url` | `http://localhost:11434` | Where the chat model is called, and this one setting decides how. An address with no path (`http://localhost:11434`, `http://host.docker.internal:11434` from a Docker container) is Ollama's own API. An address with a path (for example Gemini's `.../v1beta/openai`) is an online service in the OpenAI chat format. The key for an online service is the environment variable named in the key name of the model (for example `GEMINI_API_KEY`), never a setting |
| `chat.model` | `qwen2.5:7b` | Model that writes the answers |
| `chat.temperature` | 0.2 | Randomness. 0 gives the most repeatable answers |
| `chat.top_p` | 0.9 | Sent to Ollama |
| `chat.num_ctx` | 4096 | Context window in tokens. The system prompt, history, sections and answer must fit |
| `chat.reasoning_effort` | (empty) | Only for an online service: asks a thinking model to think less (for example `none`, `minimal`, `low`), which makes the first word arrive sooner. Empty sends nothing |
| `chat.max_tokens` | 2000 | Maximum answer length (`num_predict` for Ollama, `max_tokens` for an online service). Replies are short because the prompt asks for that, so this is only a safety limit. Keep it generous: a model that thinks counts its thinking in it, and when it is used up the chat reports that the reply was cut off (raise it, or lower `chat.reasoning_effort`) |
| `chat.history_messages` | 3 | How many recent messages the model gets. More messages mean a longer prompt and a slower reply. 1 is enough for a ticket, because the assistant's offer states the problem. A short follow-up is searched together with the previous question, taken from the whole conversation the page sends, whatever this is set to |
| `chat.system_prompt` | (long text) | The six kinds of message and the reply format. Placeholders: `{{departments}}`, `{{department_names}}` |
| `chat.format_reminder` | (short text) | Repeated at the end of the final user message. Same placeholders |

A model named in a setting must exist at its address: for Ollama it must be pulled (`ollama pull <model>` on that computer), and an online service has its own model names. Nothing writes the settings at startup: a setting with no row uses its default, and **Load default AI assistant data** (Settings, Manage App Data tab) saves all of them as rows (and resets any value we changed). So an old prompt stays until we edit it or restore the defaults.

**Making replies faster** (the time is the model reading the prompt, plus writing the answer; a hosted model may also think first): we lower `retrieval.top_k` and raise `retrieval.max_score_gap` carefully, so fewer sections are sent, lower `chat.history_messages` (1 is enough for a ticket) and `chat.max_tokens`, ask in the prompt for short answers, set `chat.reasoning_effort` for a thinking model on an API, or use a smaller or faster model. The server writes a timing line for every message (search time, time to the first text, total), which shows which step to work on.

## 13. Debugging a wrong answer

We work through it in this order. Each step rules out one part of the pipeline.

1. **Is the right section retrieved?** We call `GET /api/knowledge/search?q=<the question>` as the admin. If the right section is missing or has a low score, the problem is retrieval: the section is badly split or does not name its topic, the file was not uploaded, or the prefixes do not match the model. We fix the knowledge, not the prompt.
2. **Is it in the prompt?** If it was retrieved but the answer is wrong, we check `retrieval.min_score` (it may be dropping the section) and `retrieval.top_k`. A long section also competes for the context window (`chat.num_ctx`).
3. **Is the department right?** The `Department:` line is chosen by the model from the department descriptions in the `Departments` table. We improve the description of the department.
4. **Only then do we change the prompt.** The 7B model reacts strongly to small wording changes, so we re-test after any edit.

## 14. What we learned, and the limits

- **We describe the tone and do not paste example sentences.** When an example sentence was in the prompt, the model repeated it word for word. The one place that does contain a fixed sentence is the ticket offer, on purpose, because the wording is wanted exactly.
- **Rules per kind work better than one long rule.** One long rule sentence was ignored, while a rule for each kind of message was followed.
- **We put the format reminder last.** The model follows instructions near the end of the message best.
- **Temperature 0** gives the most repeatable answers. It does not make a 7B model deterministic across prompt changes.
- **Measured weaknesses.** The department line was right for all 14 test questions, but the **wording of "not covered" answers is unreliable**: it sometimes asserts "we do not offer X", adds advice or another contact, or stretches a nearby section. It can still write a poem when asked in a tricky way. These come from the model size and are the first things to re-test after a prompt change.
- **No server-side memory.** The conversation lives in the page and is sent with each message, so it is lost on logout or reload.
- **Changing the embedding model converts the store.** Every section is embedded again from its stored text, with the same prefix, and the old vectors are replaced together with the settings in one transaction. A failure changes nothing. It takes about a minute for 270 sections on a CPU.
- **Small sections matter.** A section that mixes two topics gets one vector that sits between them and matches neither well.

## 15. Where to look in the code

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
