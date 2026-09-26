using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Docly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeChunkEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                schema: "docly",
                table: "KnowledgeChunks",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_Embedding",
                schema: "docly",
                table: "KnowledgeChunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_Embedding",
                schema: "docly",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "Embedding",
                schema: "docly",
                table: "KnowledgeChunks");
        }
    }
}
