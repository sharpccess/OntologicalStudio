using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OntologicalStudio.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntityScenarios",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityScenarios", x => new { x.EntityId, x.ScenarioId });
                });

            migrationBuilder.CreateTable(
                name: "EntityTags",
                columns: table => new
                {
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityTags", x => new { x.EntityId, x.TagId });
                });

            migrationBuilder.CreateTable(
                name: "EntityType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedHydrationFields = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefaultTemplate = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Bidirectional = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AllowedSourceTypes = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    AllowedTargetTypes = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Universe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EntityTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Properties = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    HydrationData = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    ConfidenceLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CompletenessScore = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PositionX = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    PositionY = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entity_EntityType_EntityTypeId",
                        column: x => x.EntityTypeId,
                        principalTable: "EntityType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entity_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scenario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    UniverseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    Goals = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Results = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scenario_Universe_UniverseId",
                        column: x => x.UniverseId,
                        principalTable: "Universe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityTag",
                columns: table => new
                {
                    EntitiesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityTag", x => new { x.EntitiesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_EntityTag_Entity_EntitiesId",
                        column: x => x.EntitiesId,
                        principalTable: "Entity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityTag_Tag_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Properties = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationship_Entity_SourceEntityId",
                        column: x => x.SourceEntityId,
                        principalTable: "Entity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relationship_Entity_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "Entity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relationship_RelationshipType_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalTable: "RelationshipType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityScenario",
                columns: table => new
                {
                    EntitiesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenariosId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityScenario", x => new { x.EntitiesId, x.ScenariosId });
                    table.ForeignKey(
                        name: "FK_EntityScenario_Entity_EntitiesId",
                        column: x => x.EntitiesId,
                        principalTable: "Entity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityScenario_Scenario_ScenariosId",
                        column: x => x.ScenariosId,
                        principalTable: "Scenario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entity_EntityTypeId",
                table: "Entity",
                column: "EntityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Entity_UniverseId",
                table: "Entity",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityScenario_ScenariosId",
                table: "EntityScenario",
                column: "ScenariosId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityTag_TagsId",
                table: "EntityTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityType_Name",
                table: "EntityType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelationshipTypeId",
                table: "Relationship",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_SourceEntityId",
                table: "Relationship",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_TargetEntityId",
                table: "Relationship",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipType_Name",
                table: "RelationshipType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenario_UniverseId",
                table: "Scenario",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_Name",
                table: "Tag",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Universe_Name",
                table: "Universe",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityScenario");

            migrationBuilder.DropTable(
                name: "EntityScenarios");

            migrationBuilder.DropTable(
                name: "EntityTag");

            migrationBuilder.DropTable(
                name: "EntityTags");

            migrationBuilder.DropTable(
                name: "Relationship");

            migrationBuilder.DropTable(
                name: "Scenario");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Entity");

            migrationBuilder.DropTable(
                name: "RelationshipType");

            migrationBuilder.DropTable(
                name: "EntityType");

            migrationBuilder.DropTable(
                name: "Universe");
        }
    }
}
