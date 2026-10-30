
namespace GFGestao.API.Models;

public class Tarefa
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Responsavel { get; set; } = string.Empty;

    public string Equipe { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public string Status { get; set; } = "Pendente";
}
