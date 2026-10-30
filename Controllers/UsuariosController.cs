
using GFGestao.API.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GFGestao.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class UsuariosController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public UsuariosController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _userManager.Users
            .Select(u => new
            {
                u.Id,
                u.Nome,
                u.Email
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CriarUsuario(
        [FromBody] CadastroRequest cadastro)
    {
        if (string.IsNullOrWhiteSpace(cadastro.Nome) ||
            string.IsNullOrWhiteSpace(cadastro.Email) ||
            string.IsNullOrWhiteSpace(cadastro.Senha))
        {
            return BadRequest(new
            {
                mensagem = "Nome, e-mail e senha são obrigatórios."
            });
        }

        var email = cadastro.Email.Trim();

        var existente = await _userManager.FindByEmailAsync(email);

        if (existente != null)
        {
            return Conflict(new
            {
                mensagem = "Este e-mail já está cadastrado."
            });
        }

        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nome = cadastro.Nome.Trim(),
            EmailConfirmed = false
        };

        var resultado = await _userManager.CreateAsync(
            usuario,
            cadastro.Senha);

        if (!resultado.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível cadastrar o usuário.",
                erros = resultado.Errors.Select(e => e.Description)
            });
        }

        var atribuicao = await _userManager.AddToRoleAsync(
            usuario, "Usuario");

        if (!atribuicao.Succeeded)
        {
            await _userManager.DeleteAsync(usuario);

            return StatusCode(500, new
            {
                mensagem = "Não foi possível configurar a função do usuário."
            });
        }

        return StatusCode(201, new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            Perfil = "Usuario"
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest login)
    {
        if (string.IsNullOrWhiteSpace(login.Email) ||
            string.IsNullOrWhiteSpace(login.Senha))
        {
            return BadRequest(new
            {
                mensagem = "E-mail e senha são obrigatórios."
            });
        }

        var usuario = await _userManager.FindByEmailAsync(
            login.Email.Trim());

        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha inválidos."
            });
        }

        var resultado = await _signInManager.CheckPasswordSignInAsync(
            usuario,
            login.Senha,
            lockoutOnFailure: true);

        if (!resultado.Succeeded)
        {
            return Unauthorized(new
            {
                mensagem = "E-mail ou senha inválidos."
            });
        }

        var papeis = await _userManager.GetRolesAsync(usuario);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email ?? string.Empty)
        };

        foreach (var papel in papeis)
        {
            claims.Add(new Claim(ClaimTypes.Role, papel));
        }

        var claimsUsuario = await _userManager.GetClaimsAsync(usuario);

        foreach (var claim in claimsUsuario)
        {
            if (claim.Type == "Permissao" &&
                claim.Value == "EditarTarefas")
            {
                claims.Add(new Claim(
                    "Permissao",
                    "EditarTarefas"));
            }

            if (claim.Type == "EquipePermitida" &&
                !string.IsNullOrWhiteSpace(claim.Value))
            {
                claims.Add(new Claim(
                    "EquipePermitida",
                    claim.Value.Trim()));
            }
        }

        var chave = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(chave))
        {
            return StatusCode(500, new
            {
                mensagem = "A configuração de autenticação está incompleta."
            });
        }

        byte[] chaveBytes;

        try
        {
            chaveBytes = Convert.FromBase64String(chave);
        }
        catch (FormatException)
        {
            return StatusCode(500, new
            {
                mensagem = "A chave JWT está configurada incorretamente."
            });
        }

        if (chaveBytes.Length < 32)
        {
            return StatusCode(500, new
            {
                mensagem = "A chave JWT deve ter pelo menos 32 bytes."
            });
        }

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(chaveBytes),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credenciais);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        return Ok(new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            Perfil = papeis.FirstOrDefault() ?? "Usuario",
            token = tokenString
        });
    }

    // Somente Admin pode conceder permissões de edição por equipe.
    [HttpPost("{id}/permissao-equipe")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConcederPermissaoEquipe(
        string id,
        [FromBody] PermissaoEquipeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Equipe))
        {
            return BadRequest(new
            {
                mensagem = "Informe a equipe autorizada."
            });
        }

        var usuario = await _userManager.FindByIdAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        var equipe = request.Equipe.Trim();
        var claimsAtuais = await _userManager.GetClaimsAsync(usuario);

        if (!claimsAtuais.Any(c =>
            c.Type == "Permissao" &&
            c.Value == "EditarTarefas"))
        {
            var resultado = await _userManager.AddClaimAsync(
                usuario,
                new Claim("Permissao", "EditarTarefas"));

            if (!resultado.Succeeded)
            {
                return StatusCode(500, new
                {
                    mensagem = "Não foi possível atribuir a permissão.",
                    erros = resultado.Errors.Select(e => e.Description)
                });
            }
        }

        if (!claimsAtuais.Any(c =>
            c.Type == "EquipePermitida" &&
            string.Equals(
                c.Value,
                equipe,
                StringComparison.OrdinalIgnoreCase)))
        {
            var resultado = await _userManager.AddClaimAsync(
                usuario,
                new Claim("EquipePermitida", equipe));

            if (!resultado.Succeeded)
            {
                return StatusCode(500, new
                {
                    mensagem = "Não foi possível atribuir a equipe.",
                    erros = resultado.Errors.Select(e => e.Description)
                });
            }
        }

        return Ok(new
        {
            mensagem = "Permissão atribuída com sucesso.",
            usuarioId = usuario.Id,
            equipe
        });
    }

    // Somente Admin pode redefinir a senha de outro usuário.
    [HttpPost("{id}/redefinir-senha")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RedefinirSenha(
        string id,
        [FromBody] RedefinirSenhaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            return BadRequest(new
            {
                mensagem = "Informe a nova senha."
            });
        }

        var usuario = await _userManager.FindByIdAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        var tokenRedefinicao =
            await _userManager.GeneratePasswordResetTokenAsync(usuario);

        var resultado = await _userManager.ResetPasswordAsync(
            usuario,
            tokenRedefinicao,
            request.NovaSenha);

        if (!resultado.Succeeded)
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível redefinir a senha.",
                erros = resultado.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            mensagem = "Senha redefinida com sucesso."
        });
    }
}

public class CadastroRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class PermissaoEquipeRequest
{
    public string Equipe { get; set; } = string.Empty;
}

public class RedefinirSenhaRequest
{
    public string NovaSenha { get; set; } = string.Empty;
}
