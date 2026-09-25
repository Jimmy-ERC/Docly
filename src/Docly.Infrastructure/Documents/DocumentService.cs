using Docly.Application.Common;
using Docly.Application.Documents;
using Docly.Application.Processing;
using Docly.Application.Storage;
using Docly.Domain.Documents;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Documents;

public class DocumentService : IDocumentService
{
    private readonly DoclyDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IDocumentProcessingService _processing;

    public DocumentService(DoclyDbContext db, IFileStorageService storage, IDocumentProcessingService processing)
    {
        _db = db;
        _storage = storage;
        _processing = processing;
    }

    public async Task<PagedResult<DocumentListItemDto>> GetDocumentsAsync(
        string? search, Guid? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query =
            from d in _db.Documents.AsNoTracking()
            join c in _db.Categories.AsNoTracking() on d.CategoryId equals c.Id
            join f in _db.DocumentFiles.AsNoTracking() on d.Id equals f.DocumentId
            join u in _db.Users.AsNoTracking() on d.OwnerUserId equals u.Id
            select new { d, c.Name, f, u.FullName };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.d.Title, $"%{term}%"));
        }

        if (categoryId is not null)
        {
            query = query.Where(x => x.d.CategoryId == categoryId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.d.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DocumentListItemDto(
                x.d.Id, x.d.Title, x.d.Description, x.d.CategoryId, x.Name, x.FullName,
                x.f.OriginalFileName, x.f.SizeBytes, x.d.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<DocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await (
            from d in _db.Documents.AsNoTracking()
            join c in _db.Categories.AsNoTracking() on d.CategoryId equals c.Id
            join f in _db.DocumentFiles.AsNoTracking() on d.Id equals f.DocumentId
            join u in _db.Users.AsNoTracking() on d.OwnerUserId equals u.Id
            where d.Id == id
            select new DocumentDetailDto(
                d.Id, d.Title, d.Description, d.CategoryId, c.Name, d.OwnerUserId, u.FullName,
                f.OriginalFileName, f.ContentType, f.SizeBytes, f.UploadedAtUtc, d.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> UploadAsync(
        string title, string? description, Guid categoryId, Guid ownerUserId, UploadDocumentFile file,
        CancellationToken cancellationToken = default)
    {
        title = title.Trim();
        if (title.Length == 0)
        {
            return Result<Guid>.Failure("El título es obligatorio.");
        }

        if (!DocumentFileValidation.IsAllowed(file.ContentType, file.SizeBytes))
        {
            return Result<Guid>.Failure("El archivo no cumple el tipo o el tamaño permitido.");
        }

        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Result<Guid>.Failure("La categoría no existe.");
        }

        var documentId = Guid.NewGuid();
        var storagePath = BuildStoragePath(documentId, file.FileName);

        await _storage.UploadAsync(storagePath, file.Content, file.ContentType, cancellationToken);

        var now = DateTime.UtcNow;

        _db.Documents.Add(new Document
        {
            Id = documentId,
            Title = title,
            Description = NormalizeDescription(description),
            CategoryId = categoryId,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        _db.DocumentFiles.Add(new DocumentFile
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            StoragePath = storagePath,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            UploadedByUserId = ownerUserId,
            UploadedAtUtc = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort: don't leave an orphaned blob if the DB insert failed.
            await _storage.DeleteAsync(storagePath, CancellationToken.None);
            throw;
        }

        // Best-effort: ProcessAsync records its own failures on the job, it never throws.
        await _processing.ProcessAsync(documentId, cancellationToken);

        return Result<Guid>.Success(documentId);
    }

    public async Task<Result> UpdateMetadataAsync(
        Guid id, string title, string? description, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return Result.Failure("El documento no existe.");
        }

        title = title.Trim();
        if (title.Length == 0)
        {
            return Result.Failure("El título es obligatorio.");
        }

        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
        {
            return Result.Failure("La categoría no existe.");
        }

        document.Title = title;
        document.Description = NormalizeDescription(description);
        document.CategoryId = categoryId;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ReplaceFileAsync(
        Guid id, Guid uploadedByUserId, UploadDocumentFile file, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return Result.Failure("El documento no existe.");
        }

        if (!DocumentFileValidation.IsAllowed(file.ContentType, file.SizeBytes))
        {
            return Result.Failure("El archivo no cumple el tipo o el tamaño permitido.");
        }

        var currentFile = await _db.DocumentFiles.FirstOrDefaultAsync(f => f.DocumentId == id, cancellationToken);
        var previousPath = currentFile?.StoragePath;

        var newPath = BuildStoragePath(id, file.FileName);
        await _storage.UploadAsync(newPath, file.Content, file.ContentType, cancellationToken);

        var now = DateTime.UtcNow;

        if (currentFile is null)
        {
            _db.DocumentFiles.Add(new DocumentFile
            {
                Id = Guid.NewGuid(),
                DocumentId = id,
                StoragePath = newPath,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                UploadedByUserId = uploadedByUserId,
                UploadedAtUtc = now,
            });
        }
        else
        {
            currentFile.StoragePath = newPath;
            currentFile.OriginalFileName = file.FileName;
            currentFile.ContentType = file.ContentType;
            currentFile.SizeBytes = file.SizeBytes;
            currentFile.UploadedByUserId = uploadedByUserId;
            currentFile.UploadedAtUtc = now;
        }

        document.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken);

        // The DB row (the source of truth) is already committed to the new path; the old
        // blob is now unreferenced, so clean it up best-effort without failing the request.
        if (previousPath is not null)
        {
            await _storage.DeleteAsync(previousPath, CancellationToken.None);
        }

        // The file changed, so the previous chunks no longer match its content.
        await _processing.ProcessAsync(id, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return Result.Failure("El documento no existe.");
        }

        var file = await _db.DocumentFiles.FirstOrDefaultAsync(f => f.DocumentId == id, cancellationToken);

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(cancellationToken);

        if (file is not null)
        {
            await _storage.DeleteAsync(file.StoragePath, CancellationToken.None);
        }

        return Result.Success();
    }

    public async Task<(Stream Content, string FileName, string ContentType)?> OpenFileAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var file = await _db.DocumentFiles.AsNoTracking().FirstOrDefaultAsync(f => f.DocumentId == id, cancellationToken);
        if (file is null)
        {
            return null;
        }

        var stream = await _storage.DownloadAsync(file.StoragePath, cancellationToken);
        return (stream, file.OriginalFileName, file.ContentType);
    }

    private static string BuildStoragePath(Guid documentId, string fileName) =>
        $"{documentId}/{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
