using Microsoft.AspNetCore.Mvc;
using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Core.Models;

namespace MagmaAssessment.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]

// controller para gerenciamento de clientes (questão 3)
// implementa operações CRUD com MongoDB
public class ClientesController : ControllerBase
{
    private readonly IClienteRepository _repository;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(
        IClienteRepository repository,
        ILogger<ClientesController> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // retorna todos os clientes
    // code 200 - lista de clientes com sucesso // code 500 - erro interno ao obter clientes
    [HttpGet]
    [ProducesResponseType(typeof(List<Cliente>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Cliente>>> GetClientes()
    {
        _logger.LogInformation("GET /api/clientes - obtendo todos os clientes");

        try
        {
            var clientes = await _repository.ObterTodos();
            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao obter clientes");
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao obter clientes");
        }
    }

    // retorna um cliente especifico por id
    // name="id" - id do cliente (object id do MongoDB)
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cliente>> GetCliente(string id)
    {
        _logger.LogInformation("GET /api/clientes/{Id} - obtendo cliente por id", id);
        try
        {
            var cliente = await _repository.ObterPorId(id);
            if (cliente == null)
            {
                return NotFound(new { mensagem = $"cliente com id {id} não encontrado" });
            }
            return Ok(cliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao obter cliente por id {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao obter cliente");
        }
    }

    // retorna cliente por email
    // name="email" - email do cliente
    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cliente>> GetClientePorEmail(string email)
    {
        _logger.LogInformation("GET /api/clientes/email/{Email} - obtendo cliente por email", email);

        try
        {
            var cliente = await _repository.ObterPorEmail(email);

            if (cliente == null)
            {
                return NotFound(new { mensagem = $"cliente com email {email} não encontrado" });
            }

            return (cliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao obter cliente por email {Email}", email);
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao obter cliente");
        }
    }

    // adiciona um novo cliente
    // name="cliente" - dados do cliente no corpo da requisição
    [HttpPost]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Cliente>> PostCliente([FromBody] Cliente cliente)
    {
        _logger.LogInformation("POST /api/clientes - adicionando novo cliente: {Nome}", cliente?.Nome);

        if (cliente == null)
        {
            return BadRequest(new { mensagem = "cliente não pode ser nulo" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var clienteCriado = await _repository.Adicionar(cliente);

            _logger.LogInformation(
                "cliente criado com sucesso: ID={Id}, Nome={Nome}",
                clienteCriado.Id, clienteCriado.Nome);

            return CreatedAtAction(
                nameof(GetCliente),
                new { id = clienteCriado.Id },
                clienteCriado);
        }
        catch (InvalidOperationException ex)
        {
            // Email duplicado
            _logger.LogWarning(ex, "erro ao adicionar cliente: email duplicado");
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao adicionar cliente");
            return StatusCode(500, new { mensagem = "erro ao adicionar cliente" });
        }
    }

    // atualiza um cliente existente
    // name="id" - id do cliente a ser atualizado
    // name="clienteAtualizado" - dados atualizados do cliente no corpo da requisição
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cliente>> PutCliente(string id, [FromBody] Cliente clienteAtualizado)
    {
        _logger.LogInformation("PUT /api/clientes/{Id} - atualizando cliente", id);

        if (clienteAtualizado == null)
        {
            return BadRequest(new { mensagem = "cliente não pode ser nulo" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var atualizado = await _repository.Atualizar(id, clienteAtualizado);

            if (!atualizado)
            {
                return NotFound(new { mensagem = $"cliente com ID {id} não encontrado" });
            }

            return Ok(new { mensagem = "cliente atualizado com sucesso" });
        }
        catch (InvalidOperationException ex)
        {
            // Email duplicado
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao atualizar cliente: {Id}", id);
            return StatusCode(500, new { mensagem = "erro ao atualizar cliente" });
        }
    }

    // exclui um cliente
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCliente(string id)
    {
        _logger.LogInformation("DELETE /api/clientes/{Id} - removendo cliente", id);

        try
        {
            var removido = await _repository.Remover(id);

            if (!removido)
            {
                return NotFound(new { mensagem = $"cliente com ID {id} não encontrado" });
            }

            return Ok(new { mensagem = "cliente removido com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao remover cliente: {Id}", id);
            return StatusCode(500, new { mensagem = "erro ao remover cliente" });
        }
    }

    // verifica se um email ja esta cadastrado
    [HttpGet("verificar-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerificarEmail(string email)
    {
        _logger.LogInformation("GET /api/clientes/verificar-email/{Email} - verificando existencia do email", email);

        try
        {
            var existe = await _repository.EmailExiste(email);
            return Ok(new
            {
                email,
                existe,
                mensagem = existe ? "email já está em uso" : "email disponível"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao verificar existencia do email {Email}", email);
            return StatusCode(500, new { mensagem = "erro ao verificar email" });
        }
    }
}