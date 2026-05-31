using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OntologicalStudio.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderUsed = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModelUsed = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solution_Scenario_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionArtifact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SolutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "text/plain"),
                    InlineContent = table.Column<string>(type: "TEXT", nullable: false),
                    BlobPath = table.Column<string>(type: "TEXT", nullable: true),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    Order = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Label = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionArtifact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionArtifact_Solution_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Solution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solution_ScenarioId",
                table: "Solution",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionArtifact_SolutionId",
                table: "SolutionArtifact",
                column: "SolutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionArtifact");

            migrationBuilder.DropTable(
                name: "Solution");
        }
    }
}
