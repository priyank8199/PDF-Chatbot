# \# PDF Chatbot with .NET (100% Local RAG)

# 

# Upload a PDF, ask questions in plain English, and get answers that cite the exact file and page.

# Everything runs on your own machine: \*\*no API keys, no cloud, no monthly bill.\*\*

# 

# A .NET 10 \*"Build a PDF Chatbot with Spring AI (100% Local)"\*.

# 

# \## Features

# 

# \- \*\*Cited answers:\*\* every fact ends with its source, e.g. `\[Brightpath-Employee-Handbook.pdf, page 3]`

# \- \*\*No guessing:\*\* replies \*"I couldn't find that in your documents."\* when the answer isn't in the PDFs

# \- \*\*Streaming:\*\* answers appear word by word via Server-Sent Events (SSE)

# \- \*\*Prompt-injection hardening:\*\* instructions hidden inside a PDF are treated as data, not commands

# \- \*\*Fully local:\*\* Ollama for the models, PostgreSQL + pgvector for search, all in Docker / on your laptop

# \- \*\*pgAdmin included:\*\* browse the stored chunks in your browser

# 

# \## Tech stack

# 

# | Layer | Technology |

# |---|---|

# | Backend | .NET 10, ASP.NET Core Minimal APIs, C# |

# | PDF parsing | PdfPig |

# | Embeddings | Ollama + `nomic-embed-text` (768 dimensions) |

# | Chat model | Ollama + `gemma4` (or `llama3.2` for low-RAM machines) |

# | Vector database | PostgreSQL 17 + pgvector (HNSW index, cosine distance) |

# | Data access | Npgsql + Pgvector for .NET |

# | Front end | Plain HTML + JavaScript (`EventSource` for SSE) |

# | Infrastructure | Docker Compose (pgvector + pgAdmin) |

# 

# \## How it works

# 

# This is a \*\*RAG\*\* (Retrieval-Augmented Generation) app. The model never sees your whole PDF. It only sees the few chunks most relevant to your question.

# 

# ```

# UPLOAD:  PDF ─► pages ─► \~300-token chunks ─► embeddings ─► pgvector table

# ASK:     question ─► embedding ─► 5 closest chunks ─► prompt with <source> tags ─► gemma4 ─► streamed answer

# ```

# 

# | Job | Endpoint | Steps |

# |---|---|---|

# | Upload a PDF | `POST /api/documents` | read each page (PdfPig) → split into chunks with overlap → embed (nomic-embed-text) → store in pgvector |

# | Ask a question | `GET /api/ask?question=...` | embed the question → cosine search for the top 5 chunks → wrap them in `<source file page>` tags → stream the answer as SSE |

# 

# \## Project structure

# 

# ```

# PdfChatbot/

# ├── Program.cs               # Startup: DI registration (Postgres, Ollama), static files, endpoints

# ├── DocumentEndpoints.cs     # POST /api/documents — read → split → embed → store

# ├── ChatEndpoints.cs         # GET  /api/ask — retrieve → augment → generate (SSE) + system prompt

# ├── OllamaClient.cs          # Ollama REST client: /api/embed and streaming /api/chat (NDJSON)

# ├── VectorStore.cs           # pgvector table, insert, cosine-similarity search

# ├── Chunker.cs               # Splits text into \~225-word (\~300-token) chunks with 30-word overlap

# ├── wwwroot/index.html       # Upload + chat UI, streams the answer with EventSource

# ├── appsettings.json         # Connection string, Ollama URL, model names

# ├── compose.yaml             # pgvector (5432) + pgAdmin (5050)

# ├── db/

# │   ├── init.sql             # CREATE EXTENSION vector

# │   └── pgadmin-servers.json # Pre-registers the database in pgAdmin

# └── docs/                    # Fictional sample PDFs for testing

# &#x20;   ├── Brightpath-Employee-Handbook.pdf

# &#x20;   └── Falcon-Office-Supplies-Price-List-Q4-2026.pdf   # contains a hidden prompt-injection attack

# ```

# 

# 

# \## Getting started

# 

# \### Prerequisites

# 

# \- \[.NET 10 SDK](https://dotnet.microsoft.com/download)

# \- \[Ollama](https://ollama.com)

# \- \[Docker Desktop](https://www.docker.com/products/docker-desktop/)

# 

# \### 1. Pull the models

# 

# ```bash

# ollama pull gemma4

# ollama pull nomic-embed-text

# ```

# 

# > \*\*8 GB of RAM or no GPU?\*\* Use a smaller chat model:

# > `ollama pull llama3.2`, then set `"ChatModel": "llama3.2"` in `appsettings.json`.

# 

# \### 2. Start the database and pgAdmin

# 

# ```bash

# docker compose up -d

# ```

# 

# \### 3. Run the app

# 

# ```bash

# dotnet run --urls http://localhost:8080

# ```

# 

# If you run from Visual Studio instead, the app uses the port in `Properties/launchSettings.json`. Check the Output window for `Now listening on: http://localhost:xxxxx`.

# 

# \### 4. Try it

# 

# Open http://localhost:8080, upload a PDF from `docs/`, and ask a question.

# 

# Or use curl:

# 

# ```bash

# curl -F file=@docs/Brightpath-Employee-Handbook.pdf localhost:8080/api/documents

# curl -N -G localhost:8080/api/ask --data-urlencode "question=What is the hotel allowance per night?"

# ```

# 

# Example upload response:

# 

# ```json

# { "file": "Brightpath-Employee-Handbook.pdf", "pages": 3, "chunks": 3 }

# ```

# 

# Example streamed answer (SSE):

# 

# ```

# data:{"text":"The hotel allowance is $180 CAD per night"}

# data:{"text":" \[Brightpath-Employee-Handbook.pdf, page 3]."}

# ```

# 

# 

