using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.VarA;
using System.Collections.Concurrent;

namespace MagmaAssessment.Infrastructure.Repositories;
// implementção em memoria do repositorio de produtos (questao 2)
public class ProdutoRepository : IProdutoRepository
{
    private readonly ConcurrentDictionary<int, Produto> _produtos;
    private readonly ILogger<ProdutoRepository> _logger;
    private int _nextId = 1;

    public ProdutoRepository(ILogger<ProdutoRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _produtos = new ConcurrentDictionary<int, Produto>();

        SeedData();
    }

    private void SeedData()
    {
        _logger.LogInformation("inicializando seed data de produtos");

        var produtosIniciais = new List<Produto>
        {
            new Produto { Nome = "Notebook Dell Inspiron 15", Preco = 3500.00m },
            new Produto { Nome = "Desktop Advantech PcTop10", Preco = 2200.00m },
            new Produto { Nome = "Monitor LG TH9000 LED 27\"", Preco = 1500.00m },
            new Produto { Nome = "Teclado Logitech MX Keys", Preco = 450.00m },
            new Produto { Nome = "Mouse Logitech MX Master 3", Preco = 350.00m },
            new Produto { Nome = "Webcam Logitech C920 HD", Preco = 450.00m },
            new Produto { Nome = "Servidor Dell PowerEdge R740", Preco = 15000.00m },
            new Produto { Nome = "Switch Cisco Catalyst 2960", Preco = 3500.00m },
        };

        foreach (var produto in produtosIniciais)
        {
            produto.Id = GetNextId();
            produto.DataCriacao = DateTime.UtcNow;
            produto.Ativo = true;

            if (_produtos.TryAdd(produto.Id, produto))
            {
                _logger.LogDebug(
                    "produto seed adicionado: Id={Id}, Nome={Nome}, Preço={Preco:C}",
                    produto.Id, produto.Nome, produto.Preco);
            }
        }

        _logger.LogInformation("seed data carregado com sucesso: {Count} produtos", produtosIniciais.Count);
    }

    private int GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    public Task<List<Produto>> ObterTodos()
    {
        _logger.LogInformation("obtendo todos os produtos");

        var produtos = _produtos.Values
            .OrderBy(p => p.Id)
            .ToList();

        _logger.LogInformation("retornando {Count} produtos", produtos.Count);
        return Task.FromResult(produtos);
    }

    public Task<Produto?> ObterPorId(int id)
    {
        _logger.LogInformation("buscando produto com id: {Id}", id);

        _produtos.TryGetValue(id, out var produto);

        if (produto == null)
        {
            _logger.LogWarning("Produto com id {Id} não encontrado", id);
        }

        return Task.FromResult(produto);
    }

    public Task<Produto> Adicionar(Produto produto)
    {
        if (produto == null)
            throw new ArgumentNullException(nameof(produto));

        produto.Id = GetNextId();
        produto.DataCriacao = DateTime.UtcNow;
        produto.Ativo = true;

        if (!_produtos.TryAdd(produto.Id, produto))
        {
            _logger.LogError("falha ao adicionar produto com id {Id}", produto.Id);
            throw new InvalidOperationException($"não foi possivel adicionar o produto com id: {produto.Id}");
        }

        _logger.LogInformation("produto adicionado com sucesso: Id={Id}, Nome={Nome}, Preço={preco:C}",
        produto.Id, produto.Nome, produto.Preco);

        return Task.FromResult(produto);
    }

    public Task<bool> Atualizar(int id, Produto produtoAtualizado)
    {
        if (produtoAtualizado == null)
            throw new ArgumentNullException(nameof(produtoAtualizado));

        _logger.LogInformation("atualizando produto com id {Id}", id);

        if (!_produtos.TryGetValue(id, out var produtoExistente))
        {
            _logger.LogWarning("produto com id {Id} não encontrado para atualização", id);
            return Task.FromResult(false);
        }

        produtoAtualizado.Id = id;
        produtoAtualizado.DataCriacao = produtoExistente.DataCriacao;

        if (!_produtos.TryUpdate(id, produtoAtualizado, produtoExistente))
        {
            _logger.LogError("falha ao atualizar produto com id {Id}", id);
            return Task.FromResult(false);
        }

        _logger.LogInformation("produto atualizado com sucesso: Id={Id}, Nome={Nome}",
        id, produtoAtualizado.Nome);

        return Task.FromResult(true);
    }

    public Task<bool> Remover(int id)
    {
        _logger.LogInformation("removendo produto com id: {Id}", id);

        if (!_produtos.TryRemove(id, out var produto))
        {
            _logger.LogWarning("produto com id {Id} não encontrado para delete", id);
            return Task.FromResult(false);
        }

        _logger.LogInformation("produto removido com sucesso: Id={Id}, Nome={Nome}", id, produto.Nome);
        return Task.FromResult(true);
    }
    
    public Task<List<Produto>> ObterAtivos()
    {
        _logger.LogInformation("obtendo produto ativos");

        var produtosAtivos = _produtos.Values
            .Where(p => p.Ativo)
            .OrderBy(p => p.Id)
            .ToList();

        _logger.LogInformation("retornando {Count} produtos ativos", produtosAtivos.Count);
        return Task.FromResult(produtosAtivos);
    }
}