using System.Text;
using Docly.Application.Processing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace Docly.Infrastructure.Processing;

/// <summary>
/// Extracts plain text from the file types allowed by <see cref="Docly.Domain.Documents.DocumentFileValidation"/>.
/// PDF and DOCX parsing are CPU-bound and synchronous by nature (PdfPig/OpenXml have no async API), so
/// they run on a thread-pool thread via <see cref="Task.Run(Action)"/>.
/// </summary>
public class TextExtractionService : ITextExtractionService
{
    public Task<string> ExtractMarkdownAsync(
        Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        return contentType switch
        {
            "application/pdf" => Task.Run(() => ExtractFromPdf(content), cancellationToken),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                Task.Run(() => ExtractFromDocx(content), cancellationToken),
            "text/plain" or "text/markdown" => ExtractFromPlainTextAsync(content, cancellationToken),
            _ => throw new NotSupportedException($"Tipo de archivo no soportado para extracción: '{contentType}'."),
        };
    }

    private static string ExtractFromPdf(Stream content)
    {
        using var pdf = PdfDocument.Open(content);

        var pages = pdf.GetPages().Select(page => page.Text.Trim()).Where(text => text.Length > 0);
        return string.Join("\n\n", pages);
    }

    private static string ExtractFromDocx(Stream content)
    {
        using var document = WordprocessingDocument.Open(content, false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var paragraphs = body.Elements<Paragraph>()
            .Select(paragraph => paragraph.InnerText?.Trim() ?? string.Empty)
            .Where(text => text.Length > 0);

        return string.Join("\n\n", paragraphs);
    }

    private static async Task<string> ExtractFromPlainTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
