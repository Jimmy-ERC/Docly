using Docly.Application.Common;

namespace Docly.Application.Categories;

public sealed record CategoryListItemDto(Guid Id, string Name, string? Description);

public sealed record CategoryDetailDto(Guid Id, string Name, string? Description);

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItemDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<CategoryDetailDto?> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        string name, string? description, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid id, string name, string? description, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken = default);
}
