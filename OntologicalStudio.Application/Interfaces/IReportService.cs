using System;
using System.Threading.Tasks;

namespace OntologicalStudio.Application.Services;

public interface IReportService
{
    Task GeneratePdfReportAsync(Guid universeId, string filePath);
}
