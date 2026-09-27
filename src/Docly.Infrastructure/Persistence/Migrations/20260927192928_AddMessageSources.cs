using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Docly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessageSources",
                schema: "docly",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessageSources_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "docly",
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageSources_MessageId_Order",
                schema: "docly",
                table: "ChatMessageSources",
                columns: new[] { "MessageId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageSources",
                schema: "docly");
        }
    }
}
