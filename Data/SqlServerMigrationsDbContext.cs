
using GFGestao.API.Identity;
using GFGestao.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GFGestao.API.Data;

public class SqlServerMigrationsDbContext
    : IdentityDbContext<ApplicationUser>
{
    public SqlServerMigrationsDbContext(
        DbContextOptions<SqlServerMigrationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tarefa> Tarefas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
}
