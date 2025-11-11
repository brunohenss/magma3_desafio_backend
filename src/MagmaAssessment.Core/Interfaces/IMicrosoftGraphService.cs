using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace MagmaAssessment.Core.Interfaces
{
    public interface IMicrosoftGraphService
    {
        Task<List<User>> ObterUsuariosAsync();
        Task EnviarEmailAsync(string destinatario, string assunto, string corpo);
        Task<List<Group>> ObterGruposAsync();
    }
}
