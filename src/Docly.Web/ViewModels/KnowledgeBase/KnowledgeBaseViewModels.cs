using Docly.Application.Common;
using Docly.Application.KnowledgeBase;

namespace Docly.Web.ViewModels.KnowledgeBase;

public class KnowledgeBaseIndexViewModel
{
    public required KnowledgeBaseStatsDto Stats { get; set; }
    public required PagedResult<KnowledgeBaseDocumentDto> Documents { get; set; }
}
