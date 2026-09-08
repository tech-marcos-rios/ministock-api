using System.Net.Http.Headers;
using System.Net.Http.Json;
using MiniStock.Application.DTOs.Auth;

namespace MiniStock.Tests.Integration;

/// <summary>Helpers de auth compartidos entre las clases de test de integración.</summary>
internal static class AuthTestHelper
{
    // Credenciales del usuario Admin seedeado por DatabaseSeeder en Development.
    public const string AdminEmail = "admin@ministock.com";
    public const string AdminPassword = "Admin123!";

    public static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(AdminEmail, AdminPassword));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    /// <summary>Registra un usuario nuevo (rol User por defecto, sin aprobación) y devuelve su access token.</summary>
    public static async Task<string> RegisterAndLoginAsUserAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("Test User", email, "Password123"));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }

    public static HttpClient WithBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
