using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Data;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración y secretos
// ---------------------------------------------------------------------------
// Los secretos NO viven en appsettings.json. Se leen, en orden de prioridad, de:
//   1. Variables de entorno  (ConnectionStrings__DefaultConnection, Jwt__Key)
//      -> así se configuran en Docker, GitHub Actions y el hospedaje en la nube.
//   2. User Secrets (solo en Development) -> `dotnet user-secrets set ...`
// Si falta alguno, la API se niega a arrancar (fail fast) en vez de usar un valor inseguro.
static string RequireSetting(IConfiguration config, string key, string envVarHint)
{
    var value = config[key];
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException(
            $"Falta la configuración '{key}'. Defínala con la variable de entorno '{envVarHint}' " +
            "o con `dotnet user-secrets set` en desarrollo.");
    return value;
}

var connectionString = RequireSetting(builder.Configuration,
    "ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection");
var jwtKey = RequireSetting(builder.Configuration, "Jwt:Key", "Jwt__Key");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<TodoDbContext>();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

// Los orígenes permitidos también son configuración: cambian entre local y producción.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<INotifier, LogNotifier>();
builder.Services.AddScoped<OverdueNotificationService>();
builder.Services.AddHostedService<OverdueCheckBackgroundService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TodoDbContext>("database");

var app = builder.Build();

// En contenedores la base arranca vacía: si se habilita, se aplican las migraciones al iniciar.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    app.Logger.LogInformation("Aplicando migraciones de base de datos...");
    db.Database.Migrate();
}

// Dentro de un contenedor el TLS lo termina el proxy o el hospedaje (Render, Azure),
// por eso la redirección a HTTPS se puede apagar con HttpsRedirection__Enabled=false.
if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Sonda de salud para docker compose, el pipeline y el hospedaje.
app.MapHealthChecks("/health");

app.MapGet("/api/version", () => new
{
    version = "1.0.1",
    mensaje = "Despliegue automatico exitoso"
});

app.Run();
