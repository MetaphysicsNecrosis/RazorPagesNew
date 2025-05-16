using RazorPagesNew.ModelsDb.Reports;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportToPdfAsync(IReport report);
        Task<byte[]> ExportToExcelAsync(IReport report);
        Task<byte[]> ExportToXmlAsync(IReport report);
    }
}
