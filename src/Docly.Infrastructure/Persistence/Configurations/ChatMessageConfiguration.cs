using Docly.Domain.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Docly.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).IsRequired();

        builder.HasIndex(m => new { m.SessionId, m.CreatedAtUtc });

        builder.HasOne<ChatSession>()
            .WithMany()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
