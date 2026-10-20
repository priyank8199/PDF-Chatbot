using Npgsql;
using Pgvector;

namespace PdfChatbot;

public record Hit(string FileName, int Page, string Content);

/// <summary>Stores chunks + embeddings in PostgreSQL using pgvector.</summary>
public class VectorStore(NpgsqlDataSource db)
{
    public async Task InitializeAsync()
    {
        await using var conn = await db.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS chunks (
                id        bigserial PRIMARY KEY,
                file_name text NOT NULL,
                page      int  NOT NULL,
                content   text NOT NULL,
                embedding vector(768) NOT NULL   -- nomic-embed-text = 768 dimensions
            );
            CREATE INDEX IF NOT EXISTS chunks_embedding_idx
                ON chunks USING hnsw (embedding vector_cosine_ops);
            """, conn);
        await cmd.ExecuteNonQueryAsync();
        await conn.ReloadTypesAsync(); // make sure Npgsql knows the 'vector' type
    }

    public async Task DeleteFileAsync(string fileName)
    {
        await using var cmd = db.CreateCommand("DELETE FROM chunks WHERE file_name = @f");
        cmd.Parameters.AddWithValue("f", fileName);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddAsync(string fileName, int page, string content, float[] embedding)
    {
        await using var cmd = db.CreateCommand(
            "INSERT INTO chunks (file_name, page, content, embedding) VALUES (@f, @p, @c, @e)");
        cmd.Parameters.AddWithValue("f", fileName);
        cmd.Parameters.AddWithValue("p", page);
        cmd.Parameters.AddWithValue("c", content);
        cmd.Parameters.AddWithValue("e", new Vector(embedding));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Hit>> SearchAsync(float[] query, int topK, CancellationToken ct = default)
    {
        // <=> is cosine distance: smaller = more similar
        await using var cmd = db.CreateCommand(
            "SELECT file_name, page, content FROM chunks ORDER BY embedding <=> @q LIMIT @k");
        cmd.Parameters.AddWithValue("q", new Vector(query));
        cmd.Parameters.AddWithValue("k", topK);

        var hits = new List<Hit>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            hits.Add(new Hit(reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
        return hits;
    }
}
