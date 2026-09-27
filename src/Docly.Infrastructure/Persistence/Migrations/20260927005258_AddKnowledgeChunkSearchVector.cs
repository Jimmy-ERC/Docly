using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Docly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeChunkSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Generated column (not mapped in the EF model, on purpose: Domain stays free of full-text-search
            // concerns) that Postgres keeps in sync with "Content"; keyword search reads it with a GIN index
            // instead of running to_tsvector() over every row on each query.
            migrationBuilder.Sql(
                """
                ALTER TABLE docly."KnowledgeChunks"
                ADD COLUMN "SearchVector" tsvector GENERATED ALWAYS AS (to_tsvector('spanish', "Content")) STORED;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_KnowledgeChunks_SearchVector" ON docly."KnowledgeChunks" USING GIN ("SearchVector");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX docly."IX_KnowledgeChunks_SearchVector";""");
            migrationBuilder.Sql("""ALTER TABLE docly."KnowledgeChunks" DROP COLUMN "SearchVector";""");
        }
    }
}
