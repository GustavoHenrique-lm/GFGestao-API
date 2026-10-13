using GFGestao.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GFGestao.API.Controllers;

[Authorize(Roles = "Admin")]
public class RelatoriosController : Controller
{
    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalTarefas = await _context.Tarefas.CountAsync();

        var tarefasPendentes = await _context.Tarefas
            .CountAsync(t => t.Status == "Pendente");

        var tarefasEmAndamento = await _context.Tarefas
            .CountAsync(t => t.Status == "Em andamento");

        var tarefasConcluidas = await _context.Tarefas
            .CountAsync(t => t.Status == "Concluído");

        ViewBag.TotalTarefas = totalTarefas;
        ViewBag.TarefasPendentes = tarefasPendentes;
        ViewBag.TarefasEmAndamento = tarefasEmAndamento;
        ViewBag.TarefasConcluidas = tarefasConcluidas;

        return View();
    }
}