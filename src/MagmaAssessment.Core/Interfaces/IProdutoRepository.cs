using MagmaAssessment.Core.Models;

public interface IProdutoRepository
{
    Task<List<Produto>> ObterTodos();
    Task<Produto?> ObterPorId(int id);
    Task<Produto> Adicionar(Produto produto);
    Task<bool> Atualizar(int id, Produto produto);
    Task<bool> Remover(int id);
    Task<List<Produto>> ObterAtivos();
}