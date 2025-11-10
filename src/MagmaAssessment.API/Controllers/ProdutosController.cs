using Microsoft.AspNetCore.Mvc;
using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Core.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.Graph.Models;

namespace MagmaAssessment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoRepository _repository;
    private readonly ILogger<ProdutosController> _logger;

    public ProdutosController(
        IProdutoRepository repository,
        ILogger<ProdutosController> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // retorna todos os produtos (lista) / code 200 ok
    [HttpGet]
    [ProducesResponseType(typeof(List<Produto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Produto>>> GetProdutos()
    {
        _logger.LogInformation("GET /api/produtos - obtendo todos os produtos");
        var produtos = await _repository.ObterTodos();
        return Ok(produtos);
    }

    // retorna um produto por id / code 200 ok ou 404 not found
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Produto>> GetProduto(int id)
    {
        _logger.LogInformation("GET /api/produtos/{id} - obtendo produto por id", id);
        var produto = await _repository.ObterPorId(id);

        if (produto == null)
        {
            _logger.LogWarning("Produto com id {id} nao encontrado", id);
            return NotFound();
        }

        return Ok(produto);
    }

    // adiciona um novo produto / code 201 created
    [HttpPost]
    [ProducesResponseType(typeof(Produto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Produto>> PostProduto([FromBody] Produto produto)
    {
        _logger.LogInformation("POST /api/produtos - adicionando novo produto: {Nome}", produto?.Nome);

        if (produto == null)
        {
            return BadRequest(new { mensagem = "produto nao pode ser nulo" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var novoProduto = await _repository.Adicionar(produto);

            _logger.LogInformation("produto criado com sucesso: id={Id}, nome={Nome}", novoProduto.Id, novoProduto.Nome);

            return CreatedAtAction(
                nameof(GetProduto),
                new { id = novoProduto.Id },
                novoProduto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar produto: {Mensagem}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensagem = "Erro ao adicionar produto" });
        }
    }

    // atualiza um produto existente no banco
    // name="id" - id do produto
    // name="produto" - dados atualizados do produto
    // responses: 200 produto atualizado / 400 dados invalidos / 404 produto nao encontrado
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutProduto(int id, [FromBody] Produto produto)
    {
        _logger.LogInformation("PUT /api/produtos/{id} - atualizando produto: {nome}", id, produto?.Nome);

        if (produto == null || id != produto.Id)
        {
            return BadRequest(new { mensagem = "Dados do produto invalidos" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var atualizado = await _repository.Atualizar(id, produto);

        if (!atualizado)
        {
            return NotFound(new { mensagem = $"Produto com id {id} nao encontrado" });
        }

        return Ok(new { mensagem = "produto atualizado com sucesso: id{Id}", id });
    }

    // remove um produto
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        _logger.LogInformation("DELETE /api/produtos/{id} - removendo produto", id);

        var removido = await _repository.Remover(id);

        if (!removido)
        {
            return NotFound(new { mensagem = $"produto com id {id} nao encontrado" });
        }

        return Ok(new { mensagem = "produto removido com sucesso: id={Id}", id });
    }

    // retorna todos os produtos ativos / code 200 ok
    [HttpGet("ativos")]
    [ProducesResponseType(typeof(List<Produto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Produto>>> GetProdutosAtivos()
    {
        _logger.LogInformation("GET /api/produtos/ativos = obtendo todos os produtos ativos");

        var produtosAtivos = await _repository.ObterAtivos();

        return Ok(produtosAtivos);
    }
}