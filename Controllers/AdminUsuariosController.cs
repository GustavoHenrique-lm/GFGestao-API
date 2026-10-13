using GFGestao.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace GFGestao.API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsuariosController : Controller
{
    private readonly AppDbContext _context;

    public AdminUsuariosController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .OrderBy(u => u.Nome)
            .ToListAsync();

        return View(usuarios);
    }
}