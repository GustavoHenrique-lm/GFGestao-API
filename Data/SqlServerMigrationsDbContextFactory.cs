
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GFGestao.API.Data;

public class SqlServerMigrationsDbContextFactory
    : IDesignTimeDbContextFactory<SqlServerMigrationsDbContext>
{
    public SqlServerMigrationsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A variável ConnectionStrings__DefaultConnection não foi configurada.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<SqlServerMigrationsDbContext>();

        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(
                typeof(SqlServerMigrationsDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__EFMigrationsHistory_SQLServer"));

        return new SqlServerMigrationsDbContext(optionsBuilder.Options);
    }
}
