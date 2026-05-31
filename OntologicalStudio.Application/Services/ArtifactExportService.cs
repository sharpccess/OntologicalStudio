using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OntologicalStudio.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.RegularExpressions;

namespace OntologicalStudio.Application.Services;

public class ArtifactExportService : IArtifactExportService
{
    public async Task<ArtifactExportPayload> ExportSolutionAsync(Solution solution, ArtifactExportFormat format, CancellationToken cancellationToken = default)
    {
        var markdownContent = BuildMarkdownExportContent(solution);
        var baseName = $"{BuildSafeFileName(solution.Title)}-{DateTime.Now:yyyyMMdd-HHmmss}";

        return format switch
        {
            ArtifactExportFormat.Text => new ArtifactExportPayload
            {
                FileName = $"{baseName}.txt",
                MimeType = "text/plain",
                Content = Encoding.UTF8.GetBytes(ConvertMarkdownToPlainText(markdownContent))
            },
            ArtifactExportFormat.Markdown => new ArtifactExportPayload
            {
                FileName = $"{baseName}.md",
                MimeType = "text/markdown",
                Content = Encoding.UTF8.GetBytes(markdownContent)
            },
            ArtifactExportFormat.Pdf => await BuildBinaryExportAsync(
                $"{baseName}.pdf",
                "application/pdf",
                path => ExportPdf(path, solution.Title, markdownContent),
                cancellationToken),
            ArtifactExportFormat.Word => await BuildBinaryExportAsync(
                $"{baseName}.docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                path => ExportWord(path, solution.Title, markdownContent),
                cancellationToken),
            _ => new ArtifactExportPayload
            {
                FileName = $"{baseName}.txt",
                MimeType = "text/plain",
                Content = Encoding.UTF8.GetBytes(ConvertMarkdownToPlainText(markdownContent))
            }
        };
    }

    private static string BuildMarkdownExportContent(Solution solution)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {solution.Title}");
        sb.AppendLine();
        sb.AppendLine($"- Provider: {solution.ProviderUsed}");
        sb.AppendLine($"- Status: {solution.Status}");
        sb.AppendLine();

        foreach (var artifact in solution.Artifacts.OrderBy(x => x.Order))
        {
            sb.AppendLine($"## {artifact.Label}");
            sb.AppendLine($"_Kind: {artifact.Kind} | Mime: {artifact.MimeType}_");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(artifact.InlineContent))
            {
                if (artifact.Kind == ArtifactKind.Markdown)
                {
                    sb.AppendLine(artifact.InlineContent.Trim());
                }
                else if (artifact.Kind == ArtifactKind.Json)
                {
                    sb.AppendLine("```json");
                    sb.AppendLine(artifact.InlineContent.Trim());
                    sb.AppendLine("```");
                }
                else
                {
                    sb.AppendLine(artifact.InlineContent.Trim());
                }
            }
            else if (!string.IsNullOrWhiteSpace(artifact.BlobPath))
            {
                sb.AppendLine($"Binary artifact path: `{artifact.BlobPath}`");
            }
            else
            {
                sb.AppendLine("(empty artifact)");
            }

            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static void ExportPdf(string filePath, string title, string markdownContent)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var blocks = ParseMarkdownBlocks(markdownContent);

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Column(column =>
                {
                    foreach (var block in blocks)
                        RenderPdfBlock(column, block);
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void ExportWord(string filePath, string title, string markdownContent)
    {
        var blocks = ParseMarkdownBlocks(markdownContent);
        using var document = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
        var body = mainPart.Document.Body!;

        foreach (var block in blocks)
            AppendWordBlock(body, block);

        mainPart.Document.Save();
    }

    private static async Task<ArtifactExportPayload> BuildBinaryExportAsync(
        string fileName,
        string mimeType,
        Action<string> writeFile,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "OntologicalStudio");
        Directory.CreateDirectory(tempDirectory);

        var tempPath = Path.Combine(tempDirectory, $"{Guid.NewGuid()}-{fileName}");
        try
        {
            writeFile(tempPath);
            var bytes = await File.ReadAllBytesAsync(tempPath, cancellationToken);
            return new ArtifactExportPayload
            {
                FileName = fileName,
                MimeType = mimeType,
                Content = bytes
            };
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static string BuildSafeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(input.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "solution" : cleaned;
    }

    private static string ConvertMarkdownToPlainText(string markdownContent)
    {
        var text = markdownContent
            .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty, StringComparison.Ordinal)
            .Replace("# ", string.Empty, StringComparison.Ordinal)
            .Replace("## ", string.Empty, StringComparison.Ordinal)
            .Replace("### ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal);

        return Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "$1");
    }

    private static IReadOnlyList<MarkdownBlock> ParseMarkdownBlocks(string markdown)
    {
        var blocks = new List<MarkdownBlock>();
        var paragraphLines = new List<string>();
        var codeLines = new List<string>();
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
                return;

            blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, string.Join(" ", paragraphLines).Trim()));
            paragraphLines.Clear();
        }

        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                if (inCodeBlock)
                {
                    blocks.Add(new MarkdownBlock(MarkdownBlockKind.Code, string.Join(Environment.NewLine, codeLines)));
                    codeLines.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading3, line[4..].Trim()));
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading2, line[3..].Trim()));
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading1, line[2..].Trim()));
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, line[2..].Trim()));
                continue;
            }

            paragraphLines.Add(line);
        }

        FlushParagraph();

        if (codeLines.Count > 0)
            blocks.Add(new MarkdownBlock(MarkdownBlockKind.Code, string.Join(Environment.NewLine, codeLines)));

        return blocks;
    }

    private static void RenderPdfBlock(QuestPDF.Fluent.ColumnDescriptor column, MarkdownBlock block)
    {
        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading1:
                column.Item().PaddingBottom(8).Text(block.Text).Bold().FontSize(20);
                break;
            case MarkdownBlockKind.Heading2:
                column.Item().PaddingTop(6).PaddingBottom(4).Text(block.Text).Bold().FontSize(16);
                break;
            case MarkdownBlockKind.Heading3:
                column.Item().PaddingTop(4).PaddingBottom(3).Text(block.Text).Bold().FontSize(13);
                break;
            case MarkdownBlockKind.Bullet:
                column.Item().Row(row =>
                {
                    row.ConstantItem(14).Text("•");
                    row.RelativeItem().Text(block.Text);
                });
                break;
            case MarkdownBlockKind.Code:
                column.Item()
                    .Background("#F3F4F6")
                    .Padding(8)
                    .Text(block.Text)
                    .FontSize(9)
                    .FontFamily("Consolas");
                break;
            default:
                column.Item().Text(block.Text).FontSize(10);
                break;
        }
    }

    private static void AppendWordBlock(Body body, MarkdownBlock block)
    {
        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading1:
                body.Append(CreateWordParagraph(block.Text, "Heading1"));
                break;
            case MarkdownBlockKind.Heading2:
                body.Append(CreateWordParagraph(block.Text, "Heading2"));
                break;
            case MarkdownBlockKind.Heading3:
                body.Append(CreateWordParagraph(block.Text, "Heading3"));
                break;
            case MarkdownBlockKind.Bullet:
                body.Append(CreateWordParagraph($"• {block.Text}", null));
                break;
            case MarkdownBlockKind.Code:
                foreach (var line in block.Text.Split(Environment.NewLine))
                    body.Append(CreateWordParagraph(line, null, isCode: true));
                break;
            default:
                body.Append(CreateWordParagraph(block.Text, null));
                break;
        }
    }

    private static Paragraph CreateWordParagraph(string text, string? styleId, bool isCode = false)
    {
        var runProperties = isCode
            ? new RunProperties(new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" })
            : null;

        var run = runProperties is null
            ? new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve })
            : new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        var paragraph = new Paragraph(run);
        if (!string.IsNullOrWhiteSpace(styleId))
            paragraph.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = styleId });

        return paragraph;
    }

    private sealed record MarkdownBlock(MarkdownBlockKind Kind, string Text);

    private enum MarkdownBlockKind
    {
        Heading1,
        Heading2,
        Heading3,
        Paragraph,
        Bullet,
        Code
    }
}