using Docly.Domain.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docly.Infrastructure.Persistence.Configurations;

public class SearchQueryLogConfiguration : IEntityTypeConfiguration<SearchQueryLog>
{
    public void Configure(EntityTypeBuilder<SearchQueryLog> builder)
    {
        builder.ToTable("SearchQueryLogs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Query).IsRequired().HasMaxLength(500);

        builder.HasIndex(s => s.CreatedAtUtc);
        builder.HasIndex(s => s.UserId);
    }
}
