
using GFGestao.API.Data;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GFGestao.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class TarefasController : ControllerBase
{
    private readonly AppDbContext _context;

    public TarefasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Tarefa>>> GetTarefas()
    {
        var tarefas = await _context.Tarefas.ToListAsync();
        return Ok(tarefas);
    }

    [HttpPost]
    public async Task<ActionResult<Tarefa>> CriarTarefa(
        [FromBody] Tarefa tarefa)
    {
        _context.Tarefas.Add(tarefa);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTarefas),
            new { id = tarefa.Id },
            tarefa);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "PodeEditarTarefas")]
    public async Task<ActionResult<Tarefa>> AtualizarTarefa(
        int id,
        [FromBody] Tarefa tarefa)
    {
        if (id != tarefa.Id)
        {
            return BadRequest(new
            {
                mensagem = "O ID da rota não corresponde ao ID da tarefa."
            });
        }

        var tarefaExistente = await _context.Tarefas.FindAsync(id);

        if (tarefaExistente == null)
        {
            return NotFound(new
            {
                mensagem = "Tarefa não encontrada."
            });
        }

        if (!User.IsInRole("Admin"))
        {
            var equipesPermitidas = User
                .FindAll("EquipePermitida")
                .Select(c => c.Value.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var equipeAtual = tarefaExistente.Equipe?.Trim() ?? "";
            var novaEquipe = tarefa.Equipe?.Trim() ?? "";

            if (equipesPermitidas.Count == 0 ||
                string.IsNullOrWhiteSpace(equipeAtual) ||
                string.IsNullOrWhiteSpace(novaEquipe) ||
                !equipesPermitidas.Contains(equipeAtual) ||
                !equipesPermitidas.Contains(novaEquipe))
            {
                return Forbid("Bearer");
            }
        }

        tarefaExistente.Titulo = tarefa.Titulo;
        tarefaExistente.Descricao = tarefa.Descricao;
        tarefaExistente.Tipo = tarefa.Tipo;
        tarefaExistente.Responsavel = tarefa.Responsavel;
        tarefaExistente.Equipe = tarefa.Equipe.Trim();
        tarefaExistente.Data = tarefa.Data;
        tarefaExistente.Status = tarefa.Status;

        await _context.SaveChangesAsync();

        return Ok(tarefaExistente);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExcluirTarefa(int id)
    {
        var tarefa = await _context.Tarefas.FindAsync(id);

        if (tarefa == null)
        {
            return NotFound(new
            {
                mensagem = "Tarefa não encontrada."
            });
        }

        _context.Tarefas.Remove(tarefa);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
