using Docly.Application.Chat;
using Docly.Web.ViewModels.Documents;

namespace Docly.Web.ViewModels.Chat;

public class ChatIndexViewModel
{
    public required ChatSessionDto Session { get; set; }
    public required IReadOnlyList<ChatMessageDto> Messages { get; set; }
    public IReadOnlyList<ChatSessionDto> Sessions { get; set; } = [];
    public Guid? CategoryId { get; set; }
    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];
}
