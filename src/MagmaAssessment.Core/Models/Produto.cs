using System.ComponentModel.DataAnnotations;

namespace MagmaAssessment.Core.Models;

public class Produto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do produto é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O preço é obrigatório")]
    [Range(0.01, 999999999, ErrorMessage = "O preço deve ser maior que zero")]
    public decimal Preco { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public bool Ativo { get; set; } = true;
}