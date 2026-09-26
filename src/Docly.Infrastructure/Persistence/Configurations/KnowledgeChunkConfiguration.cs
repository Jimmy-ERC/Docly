using Docly.Application.Embeddings;
using Docly.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace Docly.Infrastructure.Persistence.Configurations;

public class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> builder)
    {
        builder.ToTable("KnowledgeChunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();

        // Domain exposes the embedding as float[]; it is stored as a pgvector column. The comparer makes
        // change tracking compare vector contents instead of array references.
        builder.Property(c => c.Embedding)
            .HasColumnType($"vector({EmbeddingModelInfo.Dimensions})")
            .HasConversion(
                v => new Vector(v),
                v => v.ToArray(),
                new ValueComparer<float[]>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    v => v.ToArray()));

        // Approximate nearest-neighbour index for cosine distance (the semantic search operator `<=>`).
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
