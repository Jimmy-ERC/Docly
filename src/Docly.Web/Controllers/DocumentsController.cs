using Docly.Application.Categories;
using Docly.Application.Documents;
using Docly.Application.Processing;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.Documents;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.DocumentsView)]
public class DocumentsController : Controller
{
    private const int PageSize = 15;

    private readonly IDocumentService _documentService;
    private readonly ICategoryService _categoryService;
    private readonly IDocumentProcessingService _processingService;

    public DocumentsController(
        IDocumentService documentService, ICategoryService categoryService,
        IDocumentProcessingService processingService)
    {
        _documentService = documentService;
        _categoryService = categoryService;
        _processingService = processingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, int page = 1)
    {
        var documents = await _documentService.GetDocumentsAsync(search, categoryId, page, PageSize);

        return View(new DocumentIndexViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Documents = documents,
            Categories = await LoadCategoryOptionsAsync(),
        });
    }

    [HttpGet]
    [HasPermission(PermissionCodes.DocumentsUpload)]
    public async Task<IActionResult> Create()
    {
        return View(new DocumentUploadViewModel { Categories = await LoadCategoryOptionsAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.DocumentsUpload)]
    public async Task<IActionResult> Create(DocumentUploadViewModel model)
    {
        if (ModelState.IsValid && model.File is not null)
        {
            await using var stream = model.File.OpenReadStream();

            var result = await _documentService.UploadAsync(
                model.Title, model.Description, model.CategoryId, User.GetUserId(),
                new UploadDocumentFile(stream, model.File.FileName, model.File.ContentType, model.File.Length));

            if (result.Succeeded)
            {
                TempData["Success"] = $"Documento '{model.Title.Trim()}' subido.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        model.Categories = await LoadCategoryOptionsAsync();
        return View(model);
    }

    [HttpGet]
    [HasPermission(PermissionCodes.DocumentsEdit)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var document = await _documentService.GetDocumentAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        return View(new DocumentEditViewModel
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            CategoryId = document.CategoryId,
            FileName = document.FileName,
            SizeBytes = document.SizeBytes,
            UploadedAtUtc = document.UploadedAtUtc,
            Processing = await _processingService.GetStatusAsync(id),
            Categories = await LoadCategoryOptionsAsync(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.DocumentsEdit)]
    public async Task<IActionResult> Edit(Guid id, DocumentEditViewModel model)
    {
        model.Id = id;

        if (ModelState.IsValid)
        {
            var result = await _documentService.UpdateMetadataAsync(
                id, model.Title, model.Description, model.CategoryId, User.GetUserId());
            if (result.Succeeded)
            {
                TempData["Success"] = "Documento actualizado.";
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result.Errors);
        }

        model.Categories = await LoadCategoryOptionsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.DocumentsEdit)]
    public async Task<IActionResult> ReplaceFile(DocumentReplaceFileViewModel model)
    {
        if (model.File is not null)
        {
            await using var stream = model.File.OpenReadStream();

            var result = await _documentService.ReplaceFileAsync(
                model.Id, User.GetUserId(),
                new UploadDocumentFile(stream, model.File.FileName, model.File.ContentType, model.File.Length));

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Archivo reemplazado." : string.Join(" ", result.Errors);
        }

        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.DocumentsEdit)]
    public async Task<IActionResult> Reprocess(Guid id)
    {
        await _processingService.ProcessAsync(id);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await _documentService.OpenFileAsync(id);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission(PermissionCodes.DocumentsDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _documentService.DeleteAsync(id, User.GetUserId());

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Documento eliminado." : string.Join(" ", result.Errors);

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<CategoryOption>> LoadCategoryOptionsAsync()
    {
        var categories = await _categoryService.GetCategoriesAsync();
        return categories.Select(c => new CategoryOption(c.Id, c.Name)).ToList();
    }

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
