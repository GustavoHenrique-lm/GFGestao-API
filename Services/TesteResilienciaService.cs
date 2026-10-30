
using System.Net;

namespace GFGestao.API.Services;

public class TesteResilienciaService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TesteResilienciaService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<object> ExecutarAsync(
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory
            .CreateClient("ServicoExterno");

        try
        {
            var response = await client.GetAsync(
                "http://localhost:5092/teste-resiliencia",
                cancellationToken);

            return new
            {
                status = (int)response.StatusCode,
                mensagem = response.IsSuccessStatusCode
                    ? "Chamada concluída."
                    : "O serviço respondeu com falha."
            };
        }
        catch (Exception ex)
            when (ex is HttpRequestException
                or TaskCanceledException
                or Polly.CircuitBreaker.BrokenCircuitException)
        {
            return new
            {
                status = (int)HttpStatusCode.ServiceUnavailable,
                mensagem =
                    "A chamada falhou após as políticas de resiliência."
            };
        }
    }
}
