using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface IImportService
{
    Task<ImportPreviewDto> PreviewAsync(Stream file, string fileName, ImportFormat format, string? institution, int financialAccountId);
    Task<ImportResultDto> ConfirmAsync(ConfirmImportRequestDto request);
    Task<List<ImportDto>> GetHistoryAsync();
    Task<ImportDto> GetByIdAsync(int id);
}