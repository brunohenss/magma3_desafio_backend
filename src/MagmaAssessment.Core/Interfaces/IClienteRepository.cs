using MagmaAssessment.Core.Models;

namespace MagmaAssessment.Core.Interfaces;

public interface IClienteRepository
{
    Task<List<Cliente>> ObterTodos();
    Task<Cliente?> ObterPorId(string id);
    Task<Cliente?> ObterPorEmail(string email);
    Task<Cliente> Adicionar(Cliente cliente);
    Task<bool> Atualizar(string id, Cliente cliente);
    Task<bool> Remover(string id);
    Task<bool> EmailExiste(string email);
}