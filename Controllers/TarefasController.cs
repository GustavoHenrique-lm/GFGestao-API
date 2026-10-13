using GFGestao.API.Data;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

    // GET: api/tarefas
    [HttpGet]
    public async Task<ActionResult<List<Tarefa>>> GetTarefas()
    {
        var tarefas = await _context.Tarefas.ToListAsync();

        return Ok(tarefas);
    }
    // POST: api/tarefas
    [HttpPost]
    public async Task<ActionResult<Tarefa>> CriarTarefa(Tarefa tarefa)
    {
    _context.Tarefas.Add(tarefa);

    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetTarefas),
        new { id = tarefa.Id },
        tarefa
    );
}
        // PUT: api/tarefas/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Tarefa>> AtualizarTarefa(
        int id,
        Tarefa tarefa)
    {
        if (id != tarefa.Id)
        {
            return BadRequest();
        }

        var tarefaExistente = await _context.Tarefas.FindAsync(id);

        if (tarefaExistente == null)
        {
            return NotFound();
        }

        tarefaExistente.Titulo = tarefa.Titulo;
        tarefaExistente.Descricao = tarefa.Descricao;
        tarefaExistente.Tipo = tarefa.Tipo;
        tarefaExistente.Responsavel = tarefa.Responsavel;
        tarefaExistente.Data = tarefa.Data;
        tarefaExistente.Status = tarefa.Status;

        await _context.SaveChangesAsync();

        return Ok(tarefaExistente);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> ExcluirTarefa(int id)
    {
        var tarefa = await _context.Tarefas.FindAsync(id);

        if (tarefa == null)
        {
            return NotFound();
        }

        _context.Tarefas.Remove(tarefa);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
