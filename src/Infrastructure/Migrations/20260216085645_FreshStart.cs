using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RAGChatbot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VectorDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    ChunkId = table.Column<string>(type: "TEXT", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Embedding = table.Column<string>(type: "TEXT", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VectorDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VectorDocuments_DocumentId",
                table: "VectorDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VectorDocuments_DocumentId_ChunkIndex",
                table: "VectorDocuments",
                columns: new[] { "DocumentId", "ChunkIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VectorDocuments");
        }
    }
}
