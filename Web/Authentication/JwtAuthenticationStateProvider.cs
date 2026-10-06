using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Web.Authentication;

public sealed class JwtAuthenticationStateProvider
    : AuthenticationStateProvider
{
    private const string TokenKey = "authToken";

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;

    public JwtAuthenticationStateProvider(
        IJSRuntime jsRuntime,
        HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    // Get the current authentication state from the stored JWT.
    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string?>(
            "authStorage.get",
            TokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return CreateAnonymousState();
        }

        var claims = ParseClaimsFromJwt(token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "jwt");

        return new AuthenticationState(
            new ClaimsPrincipal(identity));
    }

    // Store the JWT and notify Blazor that the user is authenticated.
    public async Task LoginAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync(
            "authStorage.set",
            TokenKey,
            token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    // Remove the JWT and notify Blazor that the user is anonymous.
    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync(
            "authStorage.remove",
            TokenKey);

        _httpClient.DefaultRequestHeaders.Authorization = null;

        NotifyAuthenticationStateChanged(
            Task.FromResult(CreateAnonymousState()));
    }

    // Create an authentication state for an anonymous user.
    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity()));
    }

    // Read claims from the payload section of a JWT.
    private static IEnumerable<Claim> ParseClaimsFromJwt(
        string token)
    {
        var payload = token.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);

        var claims = JsonSerializer.Deserialize<
            Dictionary<string, JsonElement>>(jsonBytes);

        if (claims is null)
        {
            return [];
        }

        return claims.Select(claim =>
            new Claim(
                claim.Key,
                claim.Value.ToString()));
    }

    // JWT uses Base64Url encoding, which may omit padding characters.
    private static byte[] ParseBase64WithoutPadding(
        string base64)
    {
        base64 = base64
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;

            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
}