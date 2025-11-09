using MagmaAssessment.Core.Models;

namespace MagmaAssessment.Core.Interfaces;

public interface IForce1Service
{
    Task<List<Ativo>> ObterTodosAtivos();
    Task<List<Ativo>> ObterComputadoresInativos();
    Task<Ativo?> ObterAtivoPorId(string id);
}