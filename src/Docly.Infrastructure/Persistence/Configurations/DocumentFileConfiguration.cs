using Docly.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docly.Infrastructure.Persistence.Configurations;

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        builder.ToTable("DocumentFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(f => f.OriginalFileName).IsRequired().HasMaxLength(255);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(150);

        // One current file per document.
        builder.HasIndex(f => f.DocumentId).IsUnique();

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(f => f.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
