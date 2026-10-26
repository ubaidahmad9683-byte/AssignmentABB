using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using TicTacToe.Api;
using TicTacToe.Api.Middleware;
using TicTacToe.Application;
using TicTacToe.Infrastructure;
using TicTacToe.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var signingKey = builder.Configuration["Authentication:Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
if (string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Jwt:Issuer"]) ||
    string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Jwt:Audience"]))
    throw new InvalidOperationException("Authentication:Jwt:Issuer and Audience must be configured.");
var tokenLifetime = builder.Configuration.GetValue<int>("Authentication:Jwt:LifetimeMinutes", 30);
if (tokenLifetime is < 1 or > 1440)
    throw new InvalidOperationException("Authentication:Jwt:LifetimeMinutes must be between 1 and 1440.");

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                return context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "A valid bearer token is required.",
                    Instance = context.Request.Path
                }, context.HttpContext.RequestAborted);
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                return context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "The authenticated user does not have permission to perform this action.",
                    Instance = context.Request.Path
                }, context.HttpContext.RequestAborted);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        })));
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var healthChecks = builder.Services.AddHealthChecks();
if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    healthChecks.AddSqlServer(builder.Configuration.GetConnectionString("GameDatabase")!, name: "sqlserver", tags: ["ready"]);
if (builder.Configuration.GetValue<bool>("Cache:UseRedis"))
    builder.Services.AddHealthChecks().AddRedis(builder.Configuration["Cache:RedisConnection"]!, name: "redis", tags: ["ready"]);
if (builder.Configuration.GetValue<bool>("Monitoring:EnableOtlp"))
{
    builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics
        .AddMeter("TicTacToe.Api")
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
}
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tic Tac Toe API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a signed JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
    app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    await db.Database.MigrateAsync();
}

if (!string.IsNullOrWhiteSpace(app.Configuration["Authentication:BootstrapAdmin:Username"]) ||
    !string.IsNullOrWhiteSpace(app.Configuration["Authentication:BootstrapAdmin:Password"]))
{
    await using var scope = app.Services.CreateAsyncScope();
    await BootstrapAdminAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();

static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration configuration)
{
    var username = configuration["Authentication:BootstrapAdmin:Username"];
    var password = configuration["Authentication:BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Configure both Authentication:BootstrapAdmin:Username and Password.");
    var db = services.GetRequiredService<GameDbContext>();
    if (await db.Users.AnyAsync(user => user.Username == username)) return;
    var user = new ApplicationUser(username, string.Empty, "Admin");
    var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
    db.Users.Add(new UserEntity
    {
        Username = username,
        Role = "Admin",
        PasswordHash = hasher.HashPassword(user, password)
    });
    await db.SaveChangesAsync();
}

public partial class Program;
