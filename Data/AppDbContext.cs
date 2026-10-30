using GFGestao.API.Models;
using GFGestao.API.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GFGestao.API.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tarefa> Tarefas { get; set; }

    public DbSet<Usuario> Usuarios { get; set; }
}