using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using MagmaAssessment.Core.Models;

namespace MagmaAssessment.Infrastructure.Services
{
    // questao 4 - implementação corrigida
    // obtem ativos por localizacao/cidade
    // duvida: na api oficial não identifiquei a implementacao real
    // mantive o formato original do desafio apenas corrigindo os erros do código
    // o codigo original usava a lib Newtonsoft.Json, troquei para System.Text.Json (mais leve e moderna)
    public class PegaAtivosService
    {
        public async Task<List<Ativo>> ObterAtivosAsync(string cidade) // precisa desserializar pra List pois não retorna apenas um, e sim uma lista.
        {
            // obs: criando HttpClient dentro do metodo não é ideal para produção
            // mas mantém a estrutura original do desafio sem impactar o restante.
            using var client = new HttpClient();

            var url = "https://api.magma-3.com/v2/Force1/GetAssets?pagination=0";

            var resposta = await client.GetAsync(url);

            // boa pratica: lança exceçao caso o status da resposta não seja 200 ok
            resposta.EnsureSuccessStatusCode();

            // await necessario ao ler conteudo da resposta
            var conteudo = await resposta.Content.ReadAsStringAsync(); // corrige nome do metodo (estava ReadAsString)

            // opções de desserialização para ignorar case-sensitive
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // desserializa a resposta em List<Ativo>
            var ativos = JsonSerializer.Deserialize<List<Ativo>>(conteudo, options);

            // retorna a lista ou uma lista vazia caso null
            return ativos ?? new List<Ativo>();
        }
    }
}
