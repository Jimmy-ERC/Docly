using Docly.Domain.Documents;

namespace Docly.Application.Statistics;

public sealed record CategoryDocumentCountDto(string CategoryName, int Count);

public sealed record ProcessingStatusCountDto(DocumentProcessingStatus? Status, int Count);

public sealed record StatsDashboardDto(
    int TotalDocuments,
    IReadOnlyList<CategoryDocumentCountDto> DocumentsByCategory,
    IReadOnlyList<ProcessingStatusCountDto> DocumentsByStatus,
    int AuditEventsLast7Days,
    int NotificationsSentLast7Days);

public interface IStatsService
{
    Task<StatsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
