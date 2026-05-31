using OntologicalStudio.Core.Models;

namespace OntologicalStudio.Application.Services;

public interface IArtifactExportService
{
    Task<ArtifactExportPayload> ExportSolutionAsync(Solution solution, ArtifactExportFormat format, CancellationToken cancellationToken = default);
}