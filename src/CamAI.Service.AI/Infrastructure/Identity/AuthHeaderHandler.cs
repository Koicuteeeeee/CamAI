using System.Net.Http.Headers;

namespace CamAI.Service.AI.Infrastructure.Identity;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IKeycloakAuthService _authService;

    public AuthHeaderHandler(IKeycloakAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
