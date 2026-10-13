using GFGestao.API.Data;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GFGestao.API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminTarefasController : Controller
{
    private readonly AppDbContext _context;

    public AdminTarefasController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var tarefas = await _context.Tarefas
            .OrderBy(t => t.Data)
            .ToListAsync();

        return View(tarefas);
    }
    [HttpGet]
    public IActionResult Create()
{
    return View();
}
    [HttpPost]
public async Task<IActionResult> Create(Tarefa tarefa)
{
    if (!ModelState.IsValid)
    {
        return View(tarefa);
    }

    _context.Tarefas.Add(tarefa);

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}
    [HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var tarefa = await _context.Tarefas.FindAsync(id);

    if (tarefa == null)
    {
        return NotFound();
    }

    return View(tarefa);
}

[HttpPost]
public async Task<IActionResult> Edit(int id, Tarefa tarefa)
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

    return RedirectToAction(nameof(Index));
}
    [HttpPost]
public async Task<IActionResult> Delete(int id)
{
    var tarefa = await _context.Tarefas.FindAsync(id);

    if (tarefa == null)
    {
        return NotFound();
    }

    _context.Tarefas.Remove(tarefa);

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}
}