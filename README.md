# PDF Chatbot with .NET — 100% Local RAG

Upload a PDF, ask questions in plain English, and get answers with citations to the exact source file and page.

**Everything runs locally on your machine — no API keys, no cloud services, and no monthly API bills.**

## Features

- **Cited answers:** Answers include source citations, such as `[Brightpath-Employee-Handbook.pdf, page 3]`.
- **Grounded responses:** If the answer cannot be found in your documents, the chatbot responds with `"I couldn't find that in your documents."`
- **Streaming responses:** Answers appear incrementally using Server-Sent Events (SSE).
- **Prompt-injection hardening:** Instructions embedded in PDFs are treated as untrusted document content, not as commands.
- **Fully local AI:** Uses Ollama for embeddings and chat generation, with PostgreSQL and pgvector for vector search.
- **Dockerized infrastructure:** Runs PostgreSQL with pgvector and pgAdmin through Docker Compose.
- **Browser-based interface:** Upload PDFs and chat with your documents through a simple HTML and JavaScript frontend.
- **No external AI APIs:** Your documents are processed locally without sending them to a hosted AI service.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Minimal APIs, C# |
| PDF parsing | PdfPig |
| Embeddings | Ollama, `nomic-embed-text` (768 dimensions) |
| Chat model | Ollama, `gemma4` or `llama3.2` |
| Vector database | PostgreSQL 17, pgvector |
| Vector indexing | HNSW index with cosine distance |
| Data access | Npgsql, Pgvector for .NET |
| Frontend | HTML, CSS, JavaScript, EventSource |
| Streaming | Server-Sent Events (SSE) |
| Infrastructure | Docker Compose |
| Database administration | pgAdmin |

## How It Works

This application uses **Retrieval-Augmented Generation (RAG)**. Instead of passing an entire PDF to the language model, it retrieves the most relevant document chunks and provides them as context for generating an answer.

### Document ingestion

```text
PDF Upload
    |
    v
Extract Text by Page (PdfPig)
    |
    v
Split Text into Overlapping Chunks
    |
    v
Generate Embeddings (Ollama)
    |
    v
Store Chunks and Vectors in PostgreSQL
```

### Question answering

```text
User Question
    |
    v
Generate Question Embedding
    |
    v
Retrieve Top 5 Relevant Chunks
    |
    v
Build Prompt with Source Metadata
    |
    v
Generate Answer with Ollama
    |
    v
Stream Answer and Page Citations via SSE
```

### API endpoints

| Operation | Endpoint | Description |
|---|---|---|
| Upload a PDF | `POST /api/documents` | Extracts page text, splits it into chunks, generates embeddings, and stores the chunks in pgvector. |
| Ask a question | `GET /api/ask?question=...` | Embeds the question, retrieves the five most relevant chunks using cosine similarity, and streams a grounded answer through SSE. |

## Project Structure

```text
PdfChatbot/
├── Program.cs                 # Application startup, dependency injection,
│                              # database configuration, static files, and endpoints
├── DocumentEndpoints.cs       # PDF upload, extraction, chunking, embedding, and storage
├── ChatEndpoints.cs           # Retrieval, prompt construction, and SSE chat responses
├── OllamaClient.cs            # Ollama REST client for embeddings and streaming chat
├── VectorStore.cs              # pgvector schema operations, inserts, and similarity search
├── Chunker.cs                  # Text chunking with overlap
├── wwwroot/
│   └── index.html              # PDF upload and streaming chat interface
├── appsettings.json            # Database connection, Ollama URL, and model settings
├── compose.yaml                # PostgreSQL/pgvector and pgAdmin services
├── db/
│   ├── init.sql                # Enables the PostgreSQL vector extension
│   └── pgadmin-servers.json    # Preconfigures the database in pgAdmin
└── docs/
    ├── Brightpath-Employee-Handbook.pdf
    └── Falcon-Office-Supplies-Price-List-Q4-2026.pdf
                                 # Sample document containing a prompt-injection test
```

## Getting Started

### Prerequisites

Install the following:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

Ensure Docker Desktop is running before starting the database services.

### 1. Download the AI models

Pull the chat and embedding models using Ollama:

```bash
ollama pull gemma4
ollama pull nomic-embed-text
```

The embedding model, `nomic-embed-text`, produces 768-dimensional vectors.

**Running on a low-memory machine?**

You can try a smaller chat model:

```bash
ollama pull llama3.2
```

Then update the chat model setting in `appsettings.json`:

```json
{
  "ChatModel": "llama3.2"
}
```

Merge this setting into your existing configuration rather than replacing other required settings.

### 2. Start PostgreSQL and pgAdmin

From the project root, run:

```bash
docker compose up -d
```

This starts the PostgreSQL database with pgvector and the pgAdmin interface, as configured in `compose.yaml`.

Check the service status:

```bash
docker compose ps
```

### 3. Run the application

From the directory containing the project file, run:

```bash
dotnet run --urls http://localhost:8080
```

Open the application in your browser:

**http://localhost:8080**

Make sure the configured Ollama URL and PostgreSQL connection string match your local environment.

## Using the API

You can also interact with the chatbot using `curl`.

### Upload a PDF

```bash
curl -F "file=@docs/Brightpath-Employee-Handbook.pdf" \
  http://localhost:8080/api/documents
```

Example response:

```json
{
  "file": "Brightpath-Employee-Handbook.pdf",
  "pages": 3,
  "chunks": 3
}
```

The values above illustrate the response format; actual page and chunk counts depend on the uploaded PDF and the chunking configuration.

### Ask a question

```bash
curl -N -G http://localhost:8080/api/ask \
  --data-urlencode "question=What is the hotel allowance per night?"
```

The `-N` option disables curl's output buffering so streamed responses can be displayed as they arrive.

Example SSE response:

```text
data:{"text":"The hotel allowance is $180 CAD per night"}

data:{"text":" [Brightpath-Employee-Handbook.pdf, page 3]."}
```

The example answer is illustrative. The actual response depends on the content of the uploaded documents.

## Configuration

Application settings are stored in `appsettings.json`.

Configure the following values according to your environment:

- **PostgreSQL connection string:** Database host, port, database name, and credentials.
- **Ollama URL:** The address of your local Ollama service.
- **Chat model:** `gemma4` or another supported Ollama chat model.
- **Embedding model:** `nomic-embed-text`.

Keep database credentials out of source control when using real credentials. Use environment variables or .NET user secrets where appropriate.

## Troubleshooting

### Ollama connection errors

- Verify that Ollama is installed and running.
- Confirm that the configured Ollama URL is correct.
- Ensure the required chat and embedding models have been pulled.

### Database connection errors

- Confirm Docker Desktop is running.
- Check the status of the services with `docker compose ps`.
- Verify the PostgreSQL connection string and port configuration.
- Review database logs with:

```bash
docker compose logs
```

### Slow responses

- Try a smaller chat model if your machine has limited RAM.
- Ensure the embedding model is available locally.
- Remember that response speed depends on your hardware, model size, and document length.

