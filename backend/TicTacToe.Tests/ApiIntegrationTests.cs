using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicTacToe.Application;
using TicTacToe.Domain;

namespace TicTacToe.Tests;

public sealed class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;
    private readonly HttpClient client;

    public ApiIntegrationTests(ApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLivenessAndCreateGameEndpointsRespond()
    {
        var healthResponse = await client.GetAsync("/health/live");
        Assert.True(healthResponse.StatusCode == HttpStatusCode.OK, await healthResponse.Content.ReadAsStringAsync());
        var response = await client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminScoreboardResetRequiresJwtRole()
    {
        var response = await client.PostAsync("/api/scoreboard/reset", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ScoreboardResetRequiresTheAdminRole()
    {
        var issuer = factory.Services.GetRequiredService<ITokenIssuer>();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/scoreboard/reset");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", issuer.Issue("player", "Player"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);

        using var adminRequest = new HttpRequestMessage(HttpMethod.Post, "/api/scoreboard/reset");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", issuer.Issue("admin", "Admin"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(adminRequest)).StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentIsAvailable()
    {
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task ProductEndpointsAreNotExposed()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/products")).StatusCode);

        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.DoesNotContain("/api/products", swagger);
    }

    [Fact]
    public async Task TokenEndpointReturnsBearerTokenForValidCredentials()
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { username = "admin", password = "test-password" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer", (await response.Content.ReadFromJsonAsync<TokenPayload>())?.TokenType);
    }

    private sealed record TokenPayload(string AccessToken, string TokenType);
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Jwt:SigningKey", "integration-test-signing-key-must-be-at-least-32-bytes");
        builder.UseSetting("Authentication:Jwt:Issuer", "test-issuer");
        builder.UseSetting("Authentication:Jwt:Audience", "test-audience");
        builder.UseSetting("ConnectionStrings:GameDatabase", "Server=(local);Database=unused;Trusted_Connection=True");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Cache:UseRedis", "false");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:GameDatabase"] = "Server=(local);Database=unused;Trusted_Connection=True",
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["Cache:UseRedis"] = "false",
            ["Authentication:Jwt:SigningKey"] = "integration-test-signing-key-must-be-at-least-32-bytes",
            ["Authentication:Jwt:Issuer"] = "test-issuer",
            ["Authentication:Jwt:Audience"] = "test-audience"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGameService>();
            services.AddSingleton<IGameService, FakeGameService>();
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, FakeAuthenticationService>();
        });
    }
}

public sealed class FakeAuthenticationService : IAuthenticationService
{
    public Task<string?> CreateTokenAsync(string username, string password, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(username == "admin" && password == "test-password" ? "test.jwt.token" : null);
}

public sealed class FakeGameService : IGameService
{
    private static GameState Empty => GameRules.New(Guid.NewGuid(), GameMode.TwoPlayer, new Scoreboard(0, 0, 0));
    public Task<GameState> CreateAsync(GameMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(GameRules.New(Guid.NewGuid(), mode, new Scoreboard(0, 0, 0)));
    public Task<GameState?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<GameState?>(Empty);
    public Task<GamePage> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(new GamePage([], page, pageSize, 0));
    public Task<GameState> PlayAsync(Guid id, Player player, int row, int column, CancellationToken cancellationToken) => Task.FromResult(Empty);
    public Task<GameState> UndoAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Empty);
    public Task<GameState> ResetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Empty);
    public Task<Scoreboard> GetScoreboardAsync(CancellationToken cancellationToken) => Task.FromResult(new Scoreboard(0, 0, 0));
    public Task<Scoreboard> ResetScoreboardAsync(CancellationToken cancellationToken) => Task.FromResult(new Scoreboard(0, 0, 0));
}
