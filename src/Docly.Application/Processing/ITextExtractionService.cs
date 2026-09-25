namespace Docly.Application.Processing;

/// <summary>Converts an uploaded document's raw bytes into plain Markdown text, the input to chunking.</summary>
public interface ITextExtractionService
{
    /// <summary>Supported content types: see <see cref="Docly.Domain.Documents.DocumentFileValidation"/>.</summary>
    Task<string> ExtractMarkdownAsync(
        Stream content, string contentType, CancellationToken cancellationToken = default);
}
