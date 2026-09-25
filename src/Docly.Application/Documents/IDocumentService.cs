using Docly.Application.Common;

namespace Docly.Application.Documents;

public sealed record DocumentListItemDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    string OwnerFullName,
    string FileName,
    long SizeBytes,
    DateTime UpdatedAtUtc);

public sealed record DocumentDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid OwnerUserId,
    string OwnerFullName,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc,
    DateTime CreatedAtUtc);

public sealed record UploadDocumentFile(Stream Content, string FileName, string ContentType, long SizeBytes);

public interface IDocumentService
{
    Task<PagedResult<DocumentListItemDto>> GetDocumentsAsync(
        string? search, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Uploads the file to Storage and creates the Document + DocumentFile rows.</summary>
    Task<Result<Guid>> UploadAsync(
        string title, string? description, Guid categoryId, Guid ownerUserId, UploadDocumentFile file,
        CancellationToken cancellationToken = default);

    /// <summary>Updates title/description/category only, the file is untouched.</summary>
    Task<Result> UpdateMetadataAsync(
        Guid id, string title, string? description, Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the current file in Storage and updates the DocumentFile row.</summary>
    Task<Result> ReplaceFileAsync(
        Guid id, Guid uploadedByUserId, UploadDocumentFile file, CancellationToken cancellationToken = default);

    /// <summary>Deletes the Document, its DocumentFile row and the object in Storage.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Opens the current file for reading, for controller-side download.</summary>
    Task<(Stream Content, string FileName, string ContentType)?> OpenFileAsync(
        Guid id, CancellationToken cancellationToken = default);
}
