using GFGestao.API.Data;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GFGestao.API.Controllers;

public class AdminLoginController : Controller
{
    private readonly AppDbContext _context;

    public AdminLoginController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(LoginRequest login)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == login.Email);

        if (usuario == null)
        {
            ViewBag.Erro = "Email ou senha inválidos.";
            return View();
        }

        bool senhaValida = BCrypt.Net.BCrypt.Verify(
            login.Senha,
            usuario.Senha
        );

        if (!senhaValida)
        {
            ViewBag.Erro = "Email ou senha inválidos.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Perfil)
        };

        var identidade = new ClaimsIdentity(
            claims,
            "Cookies"
        );

        var principal = new ClaimsPrincipal(identidade);

        await HttpContext.SignInAsync(
            "Cookies",
            principal
        );

        return Redirect("/Admin");
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");

        return Redirect("/AdminLogin");
    }
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
