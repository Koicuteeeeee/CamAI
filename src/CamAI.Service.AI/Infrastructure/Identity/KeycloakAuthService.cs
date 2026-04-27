using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CamAI.Service.AI.Infrastructure.Identity;

public interface IKeycloakAuthService
{
    Task<string?> GetAccessTokenAsync();
}

public class KeycloakAuthService : IKeycloakAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakAuthService> _logger;
    
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public KeycloakAuthService(HttpClient httpClient, IConfiguration config, ILogger<KeycloakAuthService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        // Nếu token còn hạn (trừ hao 1 phút), dùng lại token cũ
        if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
        {
            return _cachedToken;
        }

        try
        {
            _logger.LogInformation("Đang yêu cầu Access Token mới từ Keycloak...");

            var tokenUrl = _config["Keycloak:TokenUrl"];
            var clientId = _config["Keycloak:ClientId"];
            var clientSecret = _config["Keycloak:ClientSecret"];

            if (clientSecret == "BẠN_DÁN_CLIENT_SECRET_VÀO_ĐÂY")
            {
                _logger.LogWarning("Chưa cấu hình ClientSecret trong appsettings.json!");
                return null;
            }

            var dict = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId ?? "" },
                { "client_secret", clientSecret ?? "" }
            };

            var request = new FormUrlEncodedContent(dict);
            var response = await _httpClient.PostAsync(tokenUrl, request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Lỗi khi lấy Token từ Keycloak: {Error}", error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result == null) return null;

            _cachedToken = result.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);

            _logger.LogInformation("Lấy Token thành công. Hết hạn sau {Sec} giây.", result.ExpiresIn);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi gọi Keycloak");
            return null;
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
