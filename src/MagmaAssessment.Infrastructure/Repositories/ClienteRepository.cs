using MagmaAssessment.Core.Interfaces;
using MagmaAssessment.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using DocuSign.eSign.Model;

namespace MagmaAssessment.Infraestructure.Repositories;

// questão 3
// implementa repositorio de clientes usando MongoDB
// utilizando MongoDB Driver para CRUDs

public class ClienteRepository : IClienteRepository
{
    private readonly IMongoCollection<Cliente> _clientesCollection;
    private readonly ILogger<ClienteRepository> _logger;

    public ClienteRepository(
        IConfiguration configuration,
        ILogger<ClienteRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var conectionString = configuration.GetConnectionString("MongoDb")
        ?? throw new InvalidOperationException("Connection string 'MongoBd' não encontrada.");

        _logger.LogInformation("Conectando ao MongoDB: {ConnectionString}",
            MaskConnectionString(conectionString));

        var mongoClient = new MongoClient(conectionString);
        var database = mongoClient.GetDatabase("magma_assessment_db");

        _clientesCollection = database.GetCollection<Cliente>("clientes");

        CreateIndexes();

        _logger.LogInformation("Repository de clientes inicializado com sucesso");
    }

    private void CreateIndexes()
    {
        try
        {
            var emailIndexKeys = Builders<Cliente>.IndexKeys.Ascending(c => c.Email);
            var emailIndexOptions = new CreateIndexOptions { Unique = true };
            var emailIndexModel = new CreateIndexModel<Cliente>(emailIndexKeys, emailIndexOptions);

            var nomeIndexKeys = Builders<Cliente>.IndexKeys.Ascending(c => c.Nome);
            var nomeIndexModel = new CreateIndexModel<Cliente>(nomeIndexKeys);

            _clientesCollection.Indexes.CreateMany(new[] { emailIndexModel, nomeIndexModel });

            _logger.LogInformation("Indices criados para coleção de clientes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao criar indices para cliente (podem ja existir)");
            throw;
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        // mascara senha do banco nos logs da connection string
        var parts = connectionString.Split('@');
        if (parts.Length > 1)
        {
            return $"***@{parts[1]}";
        }
        return "***";
    }

    public async Task<List<Cliente>> ObterTodos()
    {
        try
        {
            _logger.LogInformation("Obtendo todos os clientes do banco de dados");
            var clientes = await _clientesCollection
                .Find(_ => true)
                .SortBy(c => c.Nome)
                .ToListAsync();

            _logger.LogInformation("Total de {Count} clientes obtidos", clientes.Count);
            return clientes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os clientes");
            throw;
        }
    }

    public async Task<Cliente?> ObterPorId(string id)
    {
        try
        {
            _logger.LogInformation("obtendo cliente por id: {Id}", id);

            var cliente = await _clientesCollection
                .Find(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                _logger.LogWarning("cliente com id: {Id} não encontrado", id);
            }

            return cliente;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao buscar cliente por id {Id}", id);
            throw;
        }
    }

    public async Task<Cliente?> ObterPorEmail(string email)
    {
        try
        {
            _logger.LogInformation("obtendo cliente por email: {Email}", email);

            var cliente = await _clientesCollection
                .Find(c => c.Email == email)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                _logger.LogWarning("cliente com email: {Email} não encontrado", email);
            }

            return cliente;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao buscar cliente por email {Email}", email);
            throw;
        }
    }

    public async Task<Cliente> Adicionar(Cliente cliente)
    {
        try
        {
            _logger.LogInformation("adicionando novo cliente: {Nome} - {Email}", cliente.Nome, cliente.Email);

            var emailExiste = await EmailExiste(cliente.Email);
            if (emailExiste)
            {
                var mensagem = $"cliente com email {cliente.Email} já existe";
                _logger.LogWarning(mensagem);
                throw new InvalidOperationException(mensagem);
            }
            await _clientesCollection.InsertOneAsync(cliente);

            _logger.LogInformation("cliente adicionado com sucesso: id={Id}", cliente.Id);
            return cliente;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao adicionar novo cliente: {Nome} - {Email}", cliente.Nome, cliente.Email);
            throw;
        }
    }

    public async Task<bool> Atualizar(string id, Cliente clienteAtualizado)
    {
        try
        {
            if (clienteAtualizado == null)
                throw new ArgumentNullException(nameof(clienteAtualizado));

            _logger.LogInformation("atualizando cliente id={Id}", id);

            var _clienteExistente = await ObterPorId(id);
            if (_clienteExistente != null &&
                _clienteExistente.Email != clienteAtualizado.Email)
            {
                var emailExiste = await EmailExiste(clienteAtualizado.Email);
                if (emailExiste)
                {
                    _logger.LogWarning("email {Email} já está em uso", clienteAtualizado.Email);
                    throw new InvalidOperationException($"email {clienteAtualizado.Email} já está em uso");
                }
            }

            var resultado = await _clientesCollection.ReplaceOneAsync(
                c => c.Id == id,
                clienteAtualizado);

            if (resultado.ModifiedCount == 0)
            {
                _logger.LogWarning("cliente com id informado não encontrado");
                return false;
            }

            _logger.LogInformation("cliente atualizado com sucesso id={id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao atualizar cliente id={Id}", id);
            throw;
        }
    }

    public async Task<bool> Remover(string id)
    {
        try
        {
            _logger.LogInformation("removendo cliente id={Id}", id);

            var resultado = await _clientesCollection.DeleteOneAsync(c => c.Id == id);

            if (resultado.DeletedCount == 0)
            {
                _logger.LogWarning("cliente com id informado não encontrado");
                return false;
            }

            _logger.LogInformation("cliente {Id} removido com sucesso", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao remover cliente id={Id}", id);
            throw;
        }
    }
    
    public async Task<bool> EmailExiste(string email)
    {
        try
        {
            var count = await _clientesCollection.CountDocumentsAsync(c => c.Email == email);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "erro ao verificar existência de email {Email}", email);
            throw;
        }
    }
}