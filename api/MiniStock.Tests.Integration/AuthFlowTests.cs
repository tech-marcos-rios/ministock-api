using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MiniStock.Application.DTOs.Auth;

namespace MiniStock.Tests.Integration;

/// <summary>
/// Prueba el flujo completo de auth vía HTTP contra la API real (no mocks): registro,
/// login, refresh y logout, más los rechazos esperados (credenciales inválidas, email
/// duplicado). Complementa (no reemplaza) los unit tests de <c>AuthService</c> — acá lo que
/// se verifica es que el pipeline completo (ValidationFilter → Controller → Service →
/// Postgres real) funcione de punta a punta.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AuthFlowTests
{
    private readonly IntegrationTestFixture _factory;

    public AuthFlowTests(IntegrationTestFixture factory) => _factory = factory;

    [Fact]
    public async Task Register_Login_Refresh_Logout_FullFlow_Succeeds()
    {
        var client = _factory.CreateClient();
        var email = $"flow-{Guid.NewGuid():N}@test.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("Flow User", email, "Password123"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registered!.Role.Should().Be("User");

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, "Password123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequest(login!.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshed!.RefreshToken.Should().NotBe(login.RefreshToken, "el refresh token rota en cada uso");

        var logoutResponse = await client.WithBearerToken(refreshed.AccessToken)
            .PostAsync("/api/v1/auth/logout", content: null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // El refresh token ya revocado no debe servir para pedir uno nuevo.
        var reuseResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new RefreshTokenRequest(refreshed.RefreshToken));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var payload = new RegisterRequest("Dup User", email, "Password123");

        await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(AuthTestHelper.AdminEmail, "esta-no-es-la-clave"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidPayload_ReturnsValidationProblemDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("", "no-es-un-email", "123"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }
}
