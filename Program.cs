
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using GFGestao.API.Data;
using GFGestao.API.Identity;
using GFGestao.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry: métricas e rastreamento.
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("GFGestao.API")
            .AddConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

// Cliente HTTP com políticas de resiliência.
builder.Services
    .AddHttpClient("ServicoExterno", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout =
            TimeSpan.FromSeconds(3);

        options.TotalRequestTimeout.Timeout =
            TimeSpan.FromSeconds(10);

        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay =
            TimeSpan.FromMilliseconds(500);

        options.CircuitBreaker.SamplingDuration =
            TimeSpan.FromSeconds(30);
        options.CircuitBreaker.MinimumThroughput = 4;
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.BreakDuration =
            TimeSpan.FromSeconds(10);

        options.Retry.OnRetry = args =>
        {
            Console.WriteLine(
                $"[RESILIÊNCIA] Retry executado. " +
                $"Tentativa: {args.AttemptNumber}");

            return default;
        };

        options.CircuitBreaker.OnOpened = args =>
        {
            Console.WriteLine(
                "[RESILIÊNCIA] Circuit breaker ABERTO.");

            return default;
        };

        options.CircuitBreaker.OnClosed = args =>
        {
            Console.WriteLine(
                "[RESILIÊNCIA] Circuit breaker FECHADO.");

            return default;
        };

        options.CircuitBreaker.OnHalfOpened = args =>
        {
            Console.WriteLine(
                "[RESILIÊNCIA] Circuit breaker HALF-OPEN: " +
                "testando recuperação.");

            return default;
        };
    });

// Conexão com o banco de dados.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=gfgestao.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsProduction())
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Health Checks: aplicação e banco de dados.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "banco-de-dados",
        tags: new[] { "ready" });

// ASP.NET Core Identity.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Cookie de autenticação do Identity.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/AdminLogin";
    options.AccessDeniedPath = "/AdminLogin/AccessDenied";

    options.Cookie.Name = "GFGestao.Identity";
    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

    options.SlidingExpiration = true;
});

// JWT para a API.
builder.Services
    .AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "Configure Jwt:Key em um armazenamento seguro.");
        }

        byte[] chaveBytes;

        try
        {
            chaveBytes = Convert.FromBase64String(jwtKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "A chave JWT não está em formato Base64 válido.");
        }

        if (chaveBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "A chave JWT deve ter pelo menos 32 bytes.");
        }

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(chaveBytes),

                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    mensagem = "Autenticação necessária."
                });
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    mensagem =
                        "Você não tem permissão para esta operação."
                });
            }
        };
    });

// MVC e API.
builder.Services.AddControllersWithViews();

// Serviço usado no teste de resiliência.
builder.Services.AddScoped<TesteResilienciaService>();

// Políticas de autorização.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PodeEditarTarefas", policy =>
    {
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(
                "Permissao",
                "EditarTarefas"));
    });
});

// CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Flutter", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

// Rate Limiting.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("LimitePublico", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

// Cache de respostas HTTP.
builder.Services.AddOutputCache();

var app = builder.Build();

// Contador temporário para testar o Output Cache.
var contadorGeracao = 0;

// Logs estruturados, correlação e duração das requisições.
app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString("N");

    context.Response.Headers["X-Correlation-ID"] =
        correlationId;

    var stopwatch =
        System.Diagnostics.Stopwatch.StartNew();

    using (app.Logger.BeginScope(
        new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.ToString()
        }))
    {
        try
        {
            await next();

            stopwatch.Stop();

            app.Logger.LogInformation(
                "Requisição concluída. StatusCode: {StatusCode}, DurationMs: {DurationMs}",
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            app.Logger.LogError(
                "Falha na requisição. ExceptionType: {ExceptionType}, DurationMs: {DurationMs}",
                ex.GetType().Name,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
});

// Inicializa o Identity e o administrador.
await IdentitySeed.InicializarAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Flutter");
app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Index}/{id?}");

// Endpoint inicial com Rate Limiting e Output Cache.
app.MapGet("/", () => Results.Json(new
{
    sistema = "GF Gestões",
    mensagem = "API funcionando corretamente",
    versao = "1.0",
    geracao = Interlocked.Increment(ref contadorGeracao)
}))
.RequireRateLimiting("LimitePublico")
.CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)));

// Endpoint temporário que simula um serviço indisponível.
app.MapGet("/teste-resiliencia", () =>
{
    return Results.Problem(
        title: "Falha simulada",
        detail: "Endpoint utilizado para testes de resiliência.",
        statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Endpoint temporário para executar a chamada com resiliência.
app.MapGet(
    "/executar-teste-resiliencia",
    async (
        TesteResilienciaService servico,
        CancellationToken cancellationToken) =>
    {
        return Results.Ok(
            await servico.ExecutarAsync(cancellationToken));
    });

// Liveness: verifica se a aplicação está em execução.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: verifica se o banco de dados está acessível.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),

    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }
});

app.Run();
