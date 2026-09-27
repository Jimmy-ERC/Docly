using Docly.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docly.Infrastructure.Persistence.Configurations;

public class MessageSourceConfiguration : IEntityTypeConfiguration<MessageSource>
{
    public void Configure(EntityTypeBuilder<MessageSource> builder)
    {
        builder.ToTable("ChatMessageSources");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DocumentTitle).IsRequired().HasMaxLength(200);
        builder.Property(s => s.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Content).IsRequired();

        // Fetches a message's citations in their prompted "[Fuente N]" order without a table scan.
        builder.HasIndex(s => new { s.MessageId, s.Order });

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(s => s.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
