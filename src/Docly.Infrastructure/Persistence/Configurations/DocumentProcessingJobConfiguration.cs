using Docly.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docly.Infrastructure.Persistence.Configurations;

public class DocumentProcessingJobConfiguration : IEntityTypeConfiguration<DocumentProcessingJob>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingJob> builder)
    {
        builder.ToTable("DocumentProcessingJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);

        // One current job per document, reset on every (re)processing run.
        builder.HasIndex(j => j.DocumentId).IsUnique();

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(j => j.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
