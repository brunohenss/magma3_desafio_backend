using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MagmaAssessment.Core.Models;

public class Cliente
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [Required(ErrorMessage = "O nome do cliente é obrigatório")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 200 caracteres")]
    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    [BsonElement("telefone")]
    public string? Telefone { get; set; }

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;
}