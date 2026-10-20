using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PdfChatbot;

/// <summary>Talks to the local Ollama server's REST API.</summary>
public class OllamaClient(HttpClient http, IConfiguration cfg, ILogger<OllamaClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string ChatModel => cfg["Ollama:ChatModel"]!;
    private string EmbedModel => cfg["Ollama:EmbedModel"]!;


    public async Task<float[][]> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct = default)
    {
        var res = await http.PostAsJsonAsync("/api/embed", new { model = EmbedModel, input = inputs }, ct);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<EmbedResponse>(Json, ct);
        return body!.Embeddings;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string system, string user, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(new
            {
                model = ChatModel,
                stream = true,
                options = new { temperature = 0.1 },
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                }
            })
        };

        using var res = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!res.IsSuccessStatusCode) // 404 "model not found" 
            throw new HttpRequestException(
                $"{(int)res.StatusCode} {await res.Content.ReadAsStringAsync(ct)}");
        res.EnsureSuccessStatusCode();
        using var reader = new StreamReader(await res.Content.ReadAsStreamAsync(ct));

        // Ollama streams one JSON object per line (NDJSON)
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;
            var chunk = JsonSerializer.Deserialize<ChatChunk>(line, Json);
            if (!string.IsNullOrEmpty(chunk?.Message?.Content)) yield return chunk.Message.Content;
            if (chunk?.Done == true) yield break;
        }
    }

    private record EmbedResponse(float[][] Embeddings);
    private record ChatChunk(ChatMessage? Message, bool Done);
    private record ChatMessage(string Role, string Content);
}
