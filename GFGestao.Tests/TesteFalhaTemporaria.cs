namespace GFGestao.Tests;

public class TesteFalhaTemporaria
{
    [Fact]
    public void TestePropositalmenteFalho()
    {
        Assert.True(false, "Falha proposital para testar o bloqueio do pipeline.");
    }
}