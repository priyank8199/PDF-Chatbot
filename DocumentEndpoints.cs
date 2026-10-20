// = DocumentController.java
using UglyToad.PdfPig;

namespace PdfChatbot;

public static class DocumentEndpoints
{
    public record UploadResult(string File, int Pages, int Chunks);

    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/documents", async (IFormFile file, OllamaClient ollama, VectorStore vectorStore) =>
        {
            // 1. Read: one entry per page, with its file name and page number
            //    (Spring: new PagePdfDocumentReader(file.getResource()).get())
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            using var pdf = PdfDocument.Open(ms.ToArray());
            var pages = pdf.GetPages()
                .Select(p => (Number: p.Number, Text: string.Join(" ", p.GetWords().Select(w => w.Text))))
                .ToList();

            // 2. Split: small chunks make search more accurate
            //    (Spring: TokenTextSplitter.builder().withChunkSize(300).build().apply(pages))
            var chunks = pages
                .SelectMany(p => Chunker.Split(p.Text).Select(text => (Page: p.Number, Text: text)))
                .ToList();

            // 3. Store: embed every chunk and save it in pgvector
            //    (Spring: vectorStore.add(chunks) — embeds and inserts in one call)
            if (chunks.Count > 0)
            {
                var vectors = await ollama.EmbedAsync(chunks.Select(c => c.Text).ToList());
                for (int i = 0; i < chunks.Count; i++)
                    await vectorStore.AddAsync(file.FileName, chunks[i].Page, chunks[i].Text, vectors[i]);
            }

            return new UploadResult(file.FileName, pages.Count, chunks.Count);
        }).DisableAntiforgery(); // file uploads from curl/plain forms have no antiforgery token
    }
}
