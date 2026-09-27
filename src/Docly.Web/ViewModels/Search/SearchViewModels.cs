using Docly.Application.Search;
using Docly.Web.ViewModels.Documents;

namespace Docly.Web.ViewModels.Search;

public class SearchViewModel
{
    public string? Query { get; set; }
    public Guid? CategoryId { get; set; }

    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];

    /// <summary>Null until the user submits a query.</summary>
    public HybridSearchResultDto? Result { get; set; }
}
