using Docly.Application.Categories;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.CategoriesManage)]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new CategoryIndexViewModel { Categories = await _categoryService.GetCategoriesAsync() });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _categoryService.CreateAsync(model.Name, model.Description);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Categoría '{model.Name.Trim()}' creada.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _categoryService.GetCategoryAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel model)
    {
        model.Id = id;

        if (ModelState.IsValid)
        {
            var result = await _categoryService.UpdateAsync(id, model.Name, model.Description);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Categoría '{model.Name.Trim()}' actualizada.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _categoryService.DeleteAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = "Categoría eliminada.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
