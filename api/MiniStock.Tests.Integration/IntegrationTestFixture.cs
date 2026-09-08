using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace MiniStock.Tests.Integration;

/// <summary>
/// Levanta la API completa (<see cref="Program"/>) contra una Postgres real en Docker
/// (Testcontainers), no contra EF Core InMemory. La app usa <c>EF.Functions.ILike</c>
/// (específico de Npgsql) en <c>ProductRepository</c> y <c>CategoryRepository</c> — InMemory
/// no sabe traducirlo, así que un test de integración que no golpea Postgres de verdad no
/// sería representativo del comportamiento real.
///
/// Se comparte entre todas las clases de test vía <see cref="IntegrationCollection"/> para
/// no pagar el costo de levantar un contenedor por clase — a cambio, las clases de esa
/// colección corren en serie (comportamiento default de xUnit para collection fixtures).
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("ministock")
        .WithUsername("ministock")
        .WithPassword("ministock123")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    // El rate limiter de /auth/* (10/min) particiona por RemoteIpAddress — en el TestServer
    // en memoria eso es siempre el mismo valor para todos los requests, así que todos los
    // tests de la colección comparten un único balde. Loguearse como Admin una sola vez por
    // corrida (en vez de una vez por test) evita agotarlo — y de paso es más realista: un
    // cliente real tampoco relogea antes de cada request.
    private string? _cachedAdminToken;
    private readonly SemaphoreSlim _adminLoginLock = new(1, 1);

    public async Task<string> GetAdminTokenAsync()
    {
        if (_cachedAdminToken is not null) return _cachedAdminToken;

        await _adminLoginLock.WaitAsync();
        try
        {
            _cachedAdminToken ??= await AuthTestHelper.LoginAsAdminAsync(CreateClient());
            return _cachedAdminToken;
        }
        finally
        {
            _adminLoginLock.Release();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development, no Production: así corre DatabaseSeeder.SeedAsync (admin@ministock.com
        // / Admin123! + categorías/productos demo), que los tests usan como fixture conocida
        // en vez de recrear un usuario Admin a mano en cada test.
        builder.UseEnvironment("Development");

        // UseSetting (no ConfigureAppConfiguration): appsettings.Development.json de
        // MiniStock.Api viaja al content root del host de test y gana la carrera contra un
        // AddInMemoryCollection agregado en ConfigureAppConfiguration — el valor placeholder
        // de esa capa (REPLACE_WITH_SECRET_KEY_MIN_32_CHARS) terminaba siendo el que usaba
        // el middleware de JWT para firmar/validar, aunque IConfiguration vía DI sí reflejaba
        // el override. UseSetting es el mecanismo que WebApplicationFactory documenta para
        // este escenario exacto y sí gana esa carrera.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-test-key-min-32-characters-long");
        builder.UseSetting("Jwt:Issuer", "ministock-api");
        builder.UseSetting("Jwt:Audience", "ministock-web");
    }
}

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration";
}
