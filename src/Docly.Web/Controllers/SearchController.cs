using Docly.Application.Categories;
using Docly.Application.Search;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Documents;
using Docly.Web.ViewModels.Search;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.DocumentsView)]
public class SearchController : Controller
{
    private readonly IHybridSearchService _search;
    private readonly ICategoryService _categoryService;

    public SearchController(IHybridSearchService search, ICategoryService categoryService)
    {
        _search = search;
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, Guid? categoryId)
    {
        var model = new SearchViewModel
        {
            Query = q,
            CategoryId = categoryId,
            Categories = (await _categoryService.GetCategoriesAsync())
                .Select(c => new CategoryOption(c.Id, c.Name))
                .ToList(),
        };

        // A plain GET without a query just shows the empty form.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var result = await _search.SearchAsync(q, categoryId, HttpContext.RequestAborted);

            if (result.Succeeded)
            {
                model.Result = result.Value;
            }
            else
            {
                // Rendered by _Alerts in this same response; reading it there also clears it.
                TempData["Error"] = string.Join(" ", result.Errors);
            }
        }

        return View(model);
    }
}
