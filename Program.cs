// = LocalRagApplication.java
// Spring Boot auto-configures the ChatClient, embedding model and VectorStore for you.
// In .NET we register them ourselves, then map the two "controllers".
using Npgsql;
using PdfChatbot;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// pgvector (PostgreSQL) — Spring: spring-ai-starter-vector-store-pgvector
var dataSourceBuilder = new NpgsqlDataSourceBuilder(cfg.GetConnectionString("Postgres"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());
builder.Services.AddSingleton<VectorStore>();

// Ollama chat + embeddings — Spring: spring-ai-starter-model-ollama
builder.Services.AddHttpClient<OllamaClient>(c =>
{
    c.BaseAddress = new Uri(cfg["Ollama:BaseUrl"]!);
    c.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();
await app.Services.GetRequiredService<VectorStore>().InitializeAsync(); // Spring: initialize-schema: true

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDocumentEndpoints(); // = DocumentController
app.MapChatEndpoints();     // = ChatController

app.Run();
