using Docly.Domain.Documents;

namespace Docly.Application.Statistics;

public sealed record CategoryDocumentCountDto(string CategoryName, int Count);

public sealed record ProcessingStatusCountDto(DocumentProcessingStatus? Status, int Count);

public sealed record RoleUserCountDto(string RoleName, int Count);

public sealed record StatsDashboardDto(
    int TotalDocuments,
    IReadOnlyList<CategoryDocumentCountDto> DocumentsByCategory,
    IReadOnlyList<ProcessingStatusCountDto> DocumentsByStatus,
    int AuditEventsLast7Days,
    int NotificationsSentLast7Days,
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    IReadOnlyList<RoleUserCountDto> UsersByRole,
    int TotalChatSessions,
    int TotalChatMessages,
    double AverageMessagesPerSession,
    int TotalKnowledgeChunks,
    double AverageChunksPerDocument,
    int SearchesLast7Days);

public interface IStatsService
{
    Task<StatsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
