using Docly.Application.KnowledgeBase;
using Docly.Domain.Authorization;
using Docly.Web.Authorization;
using Docly.Web.ViewModels.KnowledgeBase;
using Microsoft.AspNetCore.Mvc;

namespace Docly.Web.Controllers;

[HasPermission(PermissionCodes.DocumentsView)]
public class KnowledgeBaseController : Controller
{
    private const int DocumentsPageSize = 15;
    private const int ChunksPageSize = 20;

    private readonly IKnowledgeBaseService _knowledgeBase;

    public KnowledgeBaseController(IKnowledgeBaseService knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        return View(new KnowledgeBaseIndexViewModel
        {
            Stats = await _knowledgeBase.GetStatsAsync(),
            Documents = await _knowledgeBase.GetDocumentsAsync(Math.Max(page, 1), DocumentsPageSize),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Chunks(Guid id, int page = 1)
    {
        var document = await _knowledgeBase.GetDocumentChunksAsync(id, Math.Max(page, 1), ChunksPageSize);
        if (document is null)
        {
            return NotFound();
        }

        return View(document);
    }
}
