
using GFGestao.API.Identity;
using Microsoft.AspNetCore.Identity;

namespace GFGestao.API.Data;

public static class IdentitySeed
{
    public static async Task InicializarAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var services = scope.ServiceProvider;

        var roleManager =
            services.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Usuario", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var resultado = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!resultado.Succeeded)
                {
                    var erros = string.Join(
                        "; ",
                        resultado.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Erro ao criar a função {role}: {erros}");
                }
            }
        }

        var email = Environment.GetEnvironmentVariable(
            "IdentityAdmin__Email");

        var senha = Environment.GetEnvironmentVariable(
            "IdentityAdmin__Password");

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            throw new InvalidOperationException(
                "Configure IdentityAdmin__Email e IdentityAdmin__Password " +
                "nas variáveis de ambiente antes de iniciar a aplicação.");
        }

        var administrador = await userManager.FindByEmailAsync(email);

        if (administrador == null)
        {
            administrador = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nome = "Administrador",
                EmailConfirmed = true
            };

            var criacao = await userManager.CreateAsync(
                administrador, senha);

            if (!criacao.Succeeded)
            {
                var erros = string.Join(
                    "; ",
                    criacao.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Erro ao criar o administrador: {erros}");
            }
        }

        if (!await userManager.IsInRoleAsync(administrador, "Admin"))
        {
            var atribuicao = await userManager.AddToRoleAsync(
                administrador, "Admin");

            if (!atribuicao.Succeeded)
            {
                var erros = string.Join(
                    "; ",
                    atribuicao.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Erro ao atribuir a função Admin: {erros}");
            }
        }
    }
}
