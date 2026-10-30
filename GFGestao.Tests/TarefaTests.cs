using GFGestao.API.Models;

namespace GFGestao.Tests;

public class TarefaTests
{
    [Fact]
    public void CriarTarefa_DeveInicializarComValoresPadrao()
    {
        var tarefa = new Tarefa();

        Assert.Equal(0, tarefa.Id);
        Assert.Equal(string.Empty, tarefa.Titulo);
        Assert.Equal(string.Empty, tarefa.Descricao);
        Assert.Equal(string.Empty, tarefa.Tipo);
        Assert.Equal(string.Empty, tarefa.Responsavel);
        Assert.Equal(string.Empty, tarefa.Equipe);
        Assert.Equal("Pendente", tarefa.Status);
    }
}
