namespace OrcamentoFamiliar.Application.Interfaces;

public interface ICurrentFamily
{
    Task<Guid> GetFamilyIdAsync();
    Task<string?> GetUserIdAsync();
}