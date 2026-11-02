# AI and RAG, in simple words

This tab explains the ideas behind the assistant, without code: why we use AI, why we moved from fine-tuning to RAG, and what knowledge data, a vector, a vector database and RAG are. Every idea has a small example. The **Workflow** tab then shows how our app does it, step by step, with the real requests and responses. The numbers and the answers in the examples are **illustrative**.

## 1. Why we need AI in a support app

Employees ask the same kinds of questions every day: "How many leave days do I get?", "How do I connect to the VPN?", "When is the salary paid?". The people in HR, IT and Finance answer them again and again, and an employee has to wait for a person to be free.

An AI assistant can answer these questions at any time, in plain language, and in a few seconds. When it cannot solve a problem, it creates a ticket for the right department, so a person takes over.

**Example.**

- Employee: `i am not able to reset my password`
- Assistant: explains how to reset it, and offers to create a ticket for IT if that does not work.
- Employee: `that did not work`, then `yes`
- Assistant: creates the ticket and says who got it.

## 2. What an AI model knows, and what it does not

An AI model (a **language model**, or LLM) is a program that was trained on a very large amount of public text. Because of that it can read a question and write a good answer in normal language. It knows a lot about the world in general.

It does **not** know anything private about our company. It never saw our leave policy, our VPN address or the name of our IT contact.

**Example.** We ask a plain model: `How many annual leave days do I get at Ticketing AI?`

It cannot know. It may reply "Employees typically get 15 to 20 days" and sound sure. Our real policy says **18 days**. A confident wrong answer is the worst result for a support assistant. So the model needs our company's data somehow.

## 3. Fine-tuning

**Fine-tuning** means taking an existing model and training it a little more on our own examples, so that it learns a task or our data.

### Why fine-tuning is needed

A model is good at general language, not at **our** task. Fine-tuning teaches it the task, so that it behaves the way we want and knows our data.

**Example (our first version).** We fine-tuned a model on Google Colab, with past issues and the team and person that handled each one. After training, the model could read a new issue and tell us **which user and which team** to assign it to. The full write-up of that work is here: [Fine-tuning an AI model](https://www.susanpant.com.np/blogs/finetune-ai-model).

The steps were:

1. Collect examples (an issue, and who handled it).
2. Turn them into a training file.
3. Open the Colab notebook, load a base model, and train it on a GPU.
4. Save the new model and point the application at it.
5. Test it, and repeat from step 1 if the answers are not good enough.

### Pros and cons of fine-tuning

| Pros | Cons |
| --- | --- |
| The model learns a task or a style very well (for us: routing an issue to a team) | Every change in the data means training again |
| Fast to answer: no extra text is added to the question | Needs a GPU, time, and someone who knows the notebook |
| Good for fixed behaviour that does not change often | We cannot edit or delete one fact: we train a new model and hope it learned the change and did not forget something else |
| | Hard to tell where an answer came from |
| | Can still be wrong about facts it saw only a few times |

## 4. Why we needed something else: RAG

Fine-tuning worked for routing. Then we wanted the assistant to **answer questions** about the company: policies, processes, who to contact. That data changes all the time.

**Example.** The leave policy changes from 18 days to 20 days.

- With fine-tuning: prepare the new data, run Colab again, test the new model, deploy it. It takes hours, and we have to check that nothing else changed.
- With RAG: edit one file and upload it. It takes minutes, and the next answer uses the new number.

We did not want to train a model every time a document changes. So we stopped teaching the model our data, and started **giving it the right data every time it answers**. That is RAG.

## 5. What knowledge data is

**Knowledge data** is everything the assistant is allowed to answer from: the company's own documents, written as small, clear texts. In our app they are Markdown files, one per topic, each belonging to a department. Admins upload them (or write them) on the **Knowledge** page.

**Example.** One knowledge file:

```markdown
title: Leave Policy
department: HR

## Annual leave entitlement

Full-time employees receive 18 days of paid annual leave per year. Leave is counted from the first day of the Nepali fiscal year.

## Sick leave

Employees can take up to 12 days of paid sick leave per year. A medical certificate is needed for more than 2 days in a row.
```

Each `##` section is one **piece** of knowledge. A good section is about one topic and makes sense on its own, because the assistant finds and reads one section at a time, not the whole file.

Whatever is in the knowledge data is what the assistant can say. If a topic is missing, the assistant has to say it does not know, and it names the right department to contact.

## 6. What a vector is

A computer cannot compare the **meaning** of two texts. `I forgot my login` and `how do I reset my password` share almost no words, but they mean nearly the same thing. A normal text search for the word "password" would miss the first one.

An **embedding model** solves this. It is an AI model with one job: it reads a text and returns a **list of numbers** that stands for the meaning of the text. That list is called a **vector**. Texts with a similar meaning get similar numbers.

**Example** (real vectors have 768 numbers; these have 3, to make it easy to see):

| Text | Its vector |
| --- | --- |
| `how do I reset my password` | `[0.90, 0.10, 0.05]` |
| `I forgot my login` | `[0.85, 0.15, 0.08]` |
| `how many leave days do I get` | `[0.05, 0.92, 0.10]` |

The first two are close to each other, because they mean almost the same. The third is far from both, because it is about something else. Finding related texts then becomes simple: compare the numbers, and take the closest.

## 7. What a vector database is

A **vector database** is, in simple terms, **a normal database table with one extra column that holds the list of numbers, plus a function that tells how close two lists are.** That is all.

**Example.** Our `KnowledgeChunks` table:

| Heading | Text | Vector (the extra column) |
| --- | --- | --- |
| Annual leave entitlement | Full-time employees receive 18 days ... | `[0.012, -0.045, 0.101, ...]` |
| Sick leave | Employees can take up to 12 days ... | `[0.031, 0.005, -0.087, ...]` |
| Resetting your password | Use the self-service reset page at ... | `[-0.090, 0.064, 0.022, ...]` |

To search, we give the database the vector of the question, and it returns the rows with the closest vectors:

```sql
SELECT Heading, Content, vec_distance_cosine(Embedding, @vectorOfTheQuestion) AS Distance
FROM KnowledgeChunks
ORDER BY Distance      -- the smallest distance is the closest meaning
LIMIT 5;
```

- We use SQLite with a small extension, **sqlite-vec**, that adds the distance function. So we do not run a separate vector database: it is the same database file, and one extra column.
- It compares the question with every row. For hundreds or a few thousand sections that is fast.
- The readable text is stored in the same row, because the text, not the numbers, is what the chat model reads. The numbers only help us find the right rows.
- The question and the knowledge must be converted by the **same embedding model**, because numbers from two different models cannot be compared.

## 8. What RAG is

**RAG** stands for **Retrieval-Augmented Generation**:

- **Retrieval:** find the pieces of company knowledge that are related to the question.
- **Augmented:** add those pieces to the question that goes to the AI.
- **Generation:** the AI writes the answer from them.

A good picture is an **open-book exam**. A fine-tuned model is a student who studied the book before and answers from memory. With RAG the student is allowed to open the book, find the right page, and answer from it. If the book changes, the student reads the new page, and does not need to study again.

**Example.** An employee asks: `How many leave days do I get?`

1. We turn the question into a vector.
2. We find the closest sections in the database. The top one is `Annual leave entitlement`.
3. We send the AI one message: the section text, the question, and the instruction "answer only from this text".
4. The AI answers: `Full-time employees receive 18 days of paid annual leave per year.`

The model was not changed. It read the right page just before it answered.

What RAG does **not** do: it does not make the model always right. If the right section is not found, the model has nothing to answer from. If the section is unclear, the model can misread it.

## 9. Pros and cons of RAG

| Pros | Cons |
| --- | --- |
| To change what the assistant knows, upload or edit a file. No training, no GPU | The answer is only as good as the search: if the right section is not found, the answer is poor |
| Removing knowledge is deleting rows | Every question needs an extra step (embedding and search) before the model starts, so it is a little slower |
| We can see which sections were used for an answer, so a wrong answer can be traced and fixed | The sections are added to the prompt, so the prompt is longer, and a longer prompt is a slower, costlier reply |
| The model can be changed without touching the knowledge (and the knowledge without touching the model) | The model can still misread a section, so the prompt must tell it to answer only from the sections and to say so when it does not know |
| Works with a small, free model | Changing the embedding model means converting all the stored knowledge again |

## 10. Fine-tuning and RAG side by side

| | Fine-tuning | RAG (what we use now) |
| --- | --- | --- |
| Where the company knowledge lives | Inside the model | In a database, as text |
| Adding or changing knowledge | Prepare data, train again, redeploy | Upload a file |
| Removing knowledge | Train again | Delete the rows |
| Needs a GPU and training | Yes | No |
| Can show where an answer came from | No | Yes: the sections that were used are known |
| Cost of a change | Hours | Minutes |
| Best for | A fixed task or style | Facts that change |

The two are not enemies: a fine-tuned model can also be used with RAG. For our support assistant the facts change often, so RAG is the right choice.

Next, the **Workflow** tab follows the whole app: how the knowledge is prepared, loaded and embedded, and then a chat from "hello" to a created ticket.
