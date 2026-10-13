using GFGestao.API.Data;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GFGestao.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public UsuariosController(
    AppDbContext context,
    IConfiguration configuration)
{
    _context = context;
    _configuration = configuration;
}

    [HttpGet]
    public async Task<ActionResult<List<Usuario>>> GetUsuarios()
    {
        var usuarios = await _context.Usuarios.ToListAsync();

        return Ok(usuarios);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<Usuario>> CriarUsuario(Usuario usuario)
    {
        var emailExiste = await _context.Usuarios
            .AnyAsync(u => u.Email == usuario.Email);

        if (emailExiste)
        {
            return Conflict("Este e-mail já está cadastrado.");
        }
        
        usuario.Senha = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetUsuarios),
            new { id = usuario.Id },
            new
            {
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.Perfil
            }
        );
    }
    [AllowAnonymous]
    [HttpPost("login")]
public async Task<ActionResult<Usuario>> Login(LoginRequest login)
{
    var usuario = await _context.Usuarios
        .FirstOrDefaultAsync(u => u.Email == login.Email);

    if (usuario == null)
    {
        return Unauthorized("Email ou senha inválidos.");
    }

    bool senhaValida = BCrypt.Net.BCrypt.Verify(
        login.Senha,
        usuario.Senha
    );

    if (!senhaValida)
    {
        return Unauthorized("Email ou senha inválidos.");
    }

    var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
    new Claim(ClaimTypes.Name, usuario.Nome),
    new Claim(ClaimTypes.Email, usuario.Email),
    new Claim(ClaimTypes.Role, usuario.Perfil)
};

var chave = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        _configuration["Jwt:Key"]!
    )
);

var credenciais = new SigningCredentials(
    chave,
    SecurityAlgorithms.HmacSha256
);

var token = new JwtSecurityToken(
    claims: claims,
    expires: DateTime.UtcNow.AddHours(8),
    signingCredentials: credenciais
);

var tokenString = new JwtSecurityTokenHandler()
    .WriteToken(token);

return Ok(new
{
    usuario.Id,
    usuario.Nome,
    usuario.Email,
    usuario.Perfil,
    token = tokenString
});
}
}