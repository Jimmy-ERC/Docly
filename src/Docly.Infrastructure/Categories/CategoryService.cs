using Docly.Application.Categories;
using Docly.Application.Common;
using Docly.Domain.Categories;
using Docly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Docly.Infrastructure.Categories;

public class CategoryService : ICategoryService
{
    private readonly DoclyDbContext _db;

    public CategoryService(DoclyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoryListItemDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItemDto(c.Id, c.Name, c.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDetailDto?> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDetailDto(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateAsync(
        string name, string? description, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            return Result<Guid>.Failure("El nombre de la categoría es obligatorio.");
        }

        if (await NameExistsAsync(name, excludeId: null, cancellationToken))
        {
            return Result<Guid>.Failure("Ya existe una categoría con ese nombre.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeDescription(description),
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> UpdateAsync(
        Guid id, string name, string? description, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return Result.Failure("La categoría no existe.");
        }

        name = name.Trim();
        if (name.Length == 0)
        {
            return Result.Failure("El nombre de la categoría es obligatorio.");
        }

        if (await NameExistsAsync(name, excludeId: id, cancellationToken))
        {
            return Result.Failure("Ya existe una categoría con ese nombre.");
        }

        category.Name = name;
        category.Description = NormalizeDescription(description);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return Result.Failure("La categoría no existe.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLower();
        return _db.Categories
            .Where(c => excludeId == null || c.Id != excludeId)
            .AnyAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
