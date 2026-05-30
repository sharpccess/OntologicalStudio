using OntologicalStudio.Core.Interfaces;
using OntologicalStudio.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OntologicalStudio.Application.Services;

public class ReportService : IReportService
{
    private readonly IUniverseRepository _universeRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly IScenarioRepository _scenarioRepository;
    private readonly IRelationshipRepository _relationshipRepository;

    public ReportService(
        IUniverseRepository universeRepository,
        IEntityRepository entityRepository,
        IScenarioRepository scenarioRepository,
        IRelationshipRepository relationshipRepository)
    {
        _universeRepository = universeRepository;
        _entityRepository = entityRepository;
        _scenarioRepository = scenarioRepository;
        _relationshipRepository = relationshipRepository;
    }

    public async Task GeneratePdfReportAsync(Guid universeId, string filePath)
    {
        // Set the license for QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;

        var universe = await _universeRepository.GetByIdAsync(universeId);
        if (universe == null)
            throw new InvalidOperationException("Universe not found");

        var entities = await _entityRepository.GetByUniverseAsync(universeId);
        var entityList = entities.ToList();

        var scenarios = await _scenarioRepository.GetByUniverseAsync(universeId);
        var scenarioList = scenarios.ToList();

        // Get all relationships in the universe
        var relationshipsList = new List<Relationship>();
        foreach (var entity in entityList)
        {
            var rels = await _relationshipRepository.GetBySourceEntityAsync(entity.Id);
            foreach (var r in rels)
            {
                if (!relationshipsList.Any(existing => existing.Id == r.Id))
                {
                    relationshipsList.Add(r);
                }
            }
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header()
                    .Text($"Ontological Consulting Report — Universe: {universe.Name}")
                    .FontSize(8)
                    .Italic()
                    .FontColor(Colors.Grey.Medium);

                // Content
                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Header Banner
                        column.Item().Text("SYSTEMIC DIAGNOSIS & REASONING REPORT")
                            .FontSize(20)
                            .Bold()
                            .FontColor("#4f46e5");

                        column.Item().PaddingTop(2).Text($"Universe Model: {universe.Name}")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken3);

                        column.Item().PaddingTop(2).Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);

                        column.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor("#4f46e5");

                        // Description
                        if (!string.IsNullOrEmpty(universe.Description))
                        {
                            column.Item().PaddingTop(10).Text(universe.Description)
                                .FontSize(10)
                                .Italic()
                                .FontColor(Colors.Grey.Darken2);
                        }

                        // Scenarios/Situation Summary
                        if (scenarioList.Any())
                        {
                            column.Item().PaddingTop(15).Text("1. SITUATION SUMMARY & PROBLEMS")
                                .FontSize(12)
                                .Bold()
                                .FontColor("#7c3aed");

                            foreach (var scenario in scenarioList)
                            {
                                column.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Column(scCol =>
                                {
                                    scCol.Item().Text(scenario.Title).Bold().FontSize(11);
                                    if (!string.IsNullOrEmpty(scenario.Description))
                                    {
                                        scCol.Item().PaddingTop(3).Text($"Situation context: {scenario.Description}").FontSize(9);
                                    }
                                    if (!string.IsNullOrEmpty(scenario.Goals))
                                    {
                                        scCol.Item().PaddingTop(3).Text($"Objectives & Constraints: {scenario.Goals}").FontSize(9);
                                    }
                                });
                            }
                        }

                        // Key Entities
                        column.Item().PaddingTop(15).Text("2. SYSTEM ENTITIES DICTIONARY")
                            .FontSize(12)
                            .Bold()
                            .FontColor("#7c3aed");

                        column.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f); // Name
                                columns.RelativeColumn(1.5f); // Type
                                columns.RelativeColumn(5.0f); // Description
                                columns.RelativeColumn(1.0f); // Conf.
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#4f46e5").Padding(4).Text("Entity").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background("#4f46e5").Padding(4).Text("Type").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background("#4f46e5").Padding(4).Text("Description / Notes").Bold().FontColor(Colors.White).FontSize(9);
                                header.Cell().Background("#4f46e5").Padding(4).Text("Conf.").Bold().FontColor(Colors.White).FontSize(9);
                            });

                            foreach (var entity in entityList)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(entity.Name).FontSize(9);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(entity.EntityType?.Name ?? "General").FontSize(9);

                                string desc = entity.Description;
                                if (!string.IsNullOrEmpty(entity.Notes))
                                {
                                    desc += $"\nNotes: {entity.Notes}";
                                }
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(desc).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{entity.ConfidenceLevel}%").FontSize(9);
                            }
                        });

                        // Relationship Dynamics
                        if (relationshipsList.Any())
                        {
                            column.Item().PaddingTop(15).Text("3. RELATIONSHIP DYNAMICS")
                                .FontSize(12)
                                .Bold()
                                .FontColor("#7c3aed");

                            foreach (var rel in relationshipsList)
                            {
                                var sourceName = entityList.FirstOrDefault(e => e.Id == rel.SourceEntityId)?.Name ?? "Unknown";
                                var targetName = entityList.FirstOrDefault(e => e.Id == rel.TargetEntityId)?.Name ?? "Unknown";

                                column.Item().PaddingTop(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingBottom(4).Column(relCol =>
                                {
                                    relCol.Item().Text(x =>
                                    {
                                        x.Span(sourceName).Bold().FontSize(9);
                                        x.Span($" ──({rel.RelationshipType?.Name ?? "influences"})──> ").FontSize(9).FontColor("#4f46e5");
                                        x.Span(targetName).Bold().FontSize(9);
                                    });
                                    if (!string.IsNullOrEmpty(rel.Description))
                                    {
                                        relCol.Item().PaddingTop(2).Text(rel.Description).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                                    }
                                });
                            }
                        }

                        // System Reasoning Analysis (MD results)
                        foreach (var scenario in scenarioList)
                        {
                            if (!string.IsNullOrEmpty(scenario.Results) && scenario.Results != "{}")
                            {
                                column.Item().PageBreak();
                                column.Item().PaddingTop(15).Text($"4. SYSTEM DIAGNOSIS: {scenario.Title.ToUpper()}")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor("#7c3aed");

                                column.Item().PaddingTop(8).Text(scenario.Results)
                                    .FontSize(9)
                                    .LineHeight(1.15f);
                            }
                        }
                    });

                // Footer
                page.Footer()
                    .AlignRight()
                    .Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf(filePath);

        await Task.CompletedTask;
    }
}
