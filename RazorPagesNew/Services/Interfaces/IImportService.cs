using RazorPagesNew.ModelsDb.Reports;

namespace RazorPagesNew.Services.Interfaces
{
    public interface IImportService
    {
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream, ImportType importType);
        Task<ImportResult> ImportFromXmlAsync(Stream fileStream, ImportType importType);
        Task<ValidationResult> ValidateImportDataAsync(ImportData importData, ImportType importType);
    }
}
