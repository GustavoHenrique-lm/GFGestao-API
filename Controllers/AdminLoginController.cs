
using GFGestao.API.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GFGestao.API.Controllers;

public class AdminLoginController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminLoginController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        string email,
        string senha)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            ViewBag.Erro = "Informe o e-mail e a senha.";
            return View();
        }

        var usuario = await _userManager.FindByEmailAsync(
            email.Trim());

        if (usuario == null)
        {
            ViewBag.Erro = "E-mail ou senha inválidos.";
            return View();
        }

        // Verifica se a conta está bloqueada antes do login.
        if (await _userManager.IsLockedOutAsync(usuario))
        {
            ViewBag.Erro =
                "Conta temporariamente bloqueada. Tente novamente mais tarde.";
            return View();
        }

        // Verifica se o usuário possui a função administrativa.
        if (!await _userManager.IsInRoleAsync(usuario, "Admin"))
        {
            ViewBag.Erro =
                "Você não tem permissão para acessar o painel.";
            return View();
        }

        // Autentica usando o cookie padrão do ASP.NET Core Identity.
        var resultado = await _signInManager.PasswordSignInAsync(
            usuario,
            senha,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!resultado.Succeeded)
        {
            ViewBag.Erro = resultado.IsLockedOut
                ? "Conta temporariamente bloqueada. Tente novamente mais tarde."
                : "E-mail ou senha inválidos.";

            return View();
        }

        
        // O SignInManager cria o cookie do Identity.
        return Redirect("/Admin");
       
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
