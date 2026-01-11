using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using FamilyFitness.Blazor.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(
    builder.Configuration.GetSection("AzureAd"),
    OpenIdConnectDefaults.AuthenticationScheme,
    CookieAuthenticationDefaults.AuthenticationScheme)
.EnableTokenAcquisitionToCallDownstreamApi(
    initialScopes: new string[] { builder.Configuration["AzureAd:ApiScope"] ?? "" })
.AddInMemoryTokenCaches();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // Ensure we get the refresh token to enable silent token acquisition
    options.Scope.Add("offline_access");
    
    // This is critical - ensure response_type includes 'code' for proper token flow
    options.ResponseType = "code";
    
    // Save tokens in authentication properties so MSAL can access them
    options.SaveTokens = true;
    
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        // Force sign-up flow for new users
        if (context.Request.Path.StartsWithSegments("/signup"))
        {
            context.ProtocolMessage.Prompt = "login";
            context.ProtocolMessage.SetParameter("ui_locales", "en");
        }
        return Task.CompletedTask;
    };
    
    options.Events.OnTokenValidated = context =>
    {
        // Log successful authentication with better detail
        var name = context.Principal?.Identity?.Name ?? "Unknown";
        var email = context.Principal?.FindFirst("emailaddress")?.Value ??
                   context.Principal?.FindFirst("emails")?.Value ?? 
                   context.Principal?.FindFirst("email")?.Value ?? 
                   context.Principal?.FindFirst("preferred_username")?.Value ?? "No email";
        Console.WriteLine($"[AUTH] User authenticated: {name} | Email: {email}");
        
        // Log all claims for debugging
        Console.WriteLine($"[AUTH] Claims: {string.Join(" | ", context.Principal?.Claims?.Select(c => $"{c.Type.Split('/').Last()}={c.Value}") ?? new string[0])}");
        
        return Task.CompletedTask;
    };
    
    options.Events.OnAuthorizationCodeReceived = context =>
    {
        Console.WriteLine($"[AUTH] Authorization code received for user: {context.Principal?.Identity?.Name}");
        return Task.CompletedTask;
    };
    
    options.Events.OnTokenResponseReceived = context =>
    {
        Console.WriteLine($"[AUTH] Token response received - Access token present: {!string.IsNullOrEmpty(context.TokenEndpointResponse.AccessToken)}");
        Console.WriteLine($"[AUTH] Refresh token present: {!string.IsNullOrEmpty(context.TokenEndpointResponse.RefreshToken)}");
        Console.WriteLine($"[AUTH] ID token present: {!string.IsNullOrEmpty(context.TokenEndpointResponse.IdToken)}");
        
        // Critical: Log the scopes returned to ensure API scope is included
        Console.WriteLine($"[AUTH] Scopes received: {context.TokenEndpointResponse.Scope ?? "NONE"}");
        
        return Task.CompletedTask;
    };
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpContextAccessor();

// Add distributed memory cache for session support
builder.Services.AddDistributedMemoryCache();

// Add session support for tracking user provisioning
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure HttpClient for API calls with improved error handling
var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:7163";
Console.WriteLine($"[CONFIG] API URL: {apiUrl}");

builder.Services.AddScoped<ApiAccessTokenHandler>();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    Console.WriteLine($"[CONFIG] HttpClient configured for: {apiUrl}");
})
.AddHttpMessageHandler<ApiAccessTokenHandler>();

builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("API");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Add session before authentication
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Improved user provisioning middleware with better error handling and logging
app.Use(async (context, next) =>
{
    // Always log what path we're processing
    Console.WriteLine($"[MIDDLEWARE] Processing: {context.Request.Path} | User: {context.User.Identity?.Name ?? "Anonymous"} | Authenticated: {context.User.Identity?.IsAuthenticated}");
    
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // List of paths to exclude from provisioning
        var excludedPaths = new[]
        {
            "/api/me",
            "/debug",
            "/consent", 
            "/logout",
            "/signout",
            "/signin",
            "/_framework",
            "/_blazor",
            "/favicon.ico"
        };

        var currentPath = context.Request.Path.Value ?? "";
        var shouldSkip = excludedPaths.Any(path => currentPath.StartsWith(path, StringComparison.OrdinalIgnoreCase));
        var userProvisioned = context.Session.GetString("user_provisioned");

        // Allow retry for failed attempts (after 5 minutes) or if not yet attempted
        var shouldRetry = userProvisioned == null || 
                         (userProvisioned.StartsWith("failed_") && 
                          DateTime.TryParseExact(userProvisioned.Substring(7), "yyyyMMddHHmm", null, DateTimeStyles.None, out var failTime) &&
                          DateTime.UtcNow.Subtract(failTime).TotalMinutes > 5) ||
                         (userProvisioned.StartsWith("needs_consent_") && 
                          DateTime.TryParseExact(userProvisioned.Substring(14), "yyyyMMddHHmm", null, DateTimeStyles.None, out var consentTime) &&
                          DateTime.UtcNow.Subtract(consentTime).TotalMinutes > 5);

        // Special handling for persistent MSAL account issues
        var requiresReauth = context.Items.ContainsKey("RequiresReauth");
        
        if (requiresReauth)
        {
            Console.WriteLine($"[PROVISION] MSAL account association failed - forcing re-authentication");
            context.Session.Clear(); // Clear all session data
            
            // Force fresh authentication by signing out and back in
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = context.Request.Path
            });
            return;
        }

        if (!shouldSkip && shouldRetry)
        {
            Console.WriteLine($"[PROVISION] Starting auto-provisioning for user: {context.User.Identity.Name}");
            Console.WriteLine($"[PROVISION] Previous attempt: {userProvisioned ?? "none"}");
            Console.WriteLine($"[PROVISION] User claims: {string.Join(" | ", context.User.Claims.Select(c => $"{c.Type.Split('/').Last()}={c.Value}"))}");
            
            try
            {
                var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("API");
                
                if (httpClient.BaseAddress == null)
                {
                    Console.WriteLine($"[PROVISION ERROR] HttpClient BaseAddress is null! Cannot provision user.");
                }
                else
                {
                    Console.WriteLine($"[PROVISION] Making request to: {httpClient.BaseAddress}api/me");
                    
                    // Add timeout to prevent hanging
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    
                    var response = await httpClient.GetAsync("api/me", cts.Token);
                    var content = await response.Content.ReadAsStringAsync(cts.Token);
                    
                    Console.WriteLine($"[PROVISION] Response: {response.StatusCode}");
                    Console.WriteLine($"[PROVISION] Content: {content}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[PROVISION SUCCESS] User provisioned successfully!");
                        context.Session.SetString("user_provisioned", "true");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine($"[PROVISION FAILED] 401 Unauthorized - Token issue detected");
                        
                        // Count how many times we've tried
                        var attemptCount = 0;
                        if (userProvisioned?.StartsWith("needs_reauth_") == true)
                        {
                            int.TryParse(userProvisioned.Substring(13), out attemptCount);
                        }
                        attemptCount++;
                        
                        if (attemptCount > 3)
                        {
                            // After 3 attempts, force complete re-authentication
                            Console.WriteLine($"[PROVISION] Multiple auth failures. Forcing fresh authentication.");
                            context.Session.Clear();
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                            {
                                RedirectUri = context.Request.Path
                            });
                            return;
                        }
                        else
                        {
                            // Mark as needing re-auth and trigger silent re-authentication
                            context.Session.SetString("user_provisioned", $"needs_reauth_{attemptCount}");
                            Console.WriteLine($"[PROVISION] Triggering re-authentication (attempt {attemptCount})");
                            
                            // Challenge will trigger token refresh automatically
                            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                            {
                                RedirectUri = context.Request.Path
                            });
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[PROVISION FAILED] Status: {response.StatusCode}, Content: {content}");
                        context.Session.SetString("user_provisioned", $"failed_{DateTime.UtcNow:yyyyMMddHHmm}");
                    }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Console.WriteLine($"[PROVISION ERROR] Timeout: {ex.Message}");
                context.Session.SetString("user_provisioned", $"timeout_{DateTime.UtcNow:yyyyMMddHHmm}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[PROVISION ERROR] HTTP error: {ex.Message}");
                context.Session.SetString("user_provisioned", $"http_error_{DateTime.UtcNow:yyyyMMddHHmm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PROVISION ERROR] Unexpected error: {ex.Message}");
                Console.WriteLine($"[PROVISION ERROR] Stack trace: {ex.StackTrace}");
                context.Session.SetString("user_provisioned", $"error_{DateTime.UtcNow:yyyyMMddHHmm}");
            }
        }
        else if (shouldSkip)
        {
            Console.WriteLine($"[PROVISION] Skipping provisioning for excluded path: {currentPath}");
        }
        else if (userProvisioned != null)
        {
            Console.WriteLine($"[PROVISION] User already processed: {userProvisioned}");
        }
    }
    
    await next();
});

app.UseAntiforgery();

// Authentication endpoints
app.MapGet("/consent", async context =>
{
    // Legacy endpoint - now just does a regular re-authentication
    Console.WriteLine("[AUTH] Consent endpoint called - redirecting to signin");
    context.Response.Redirect("/signin");
});

app.MapGet("/signup", async context =>
{
    Console.WriteLine("[AUTH] Signup endpoint called");
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.MapGet("/signin", async context =>
{
    Console.WriteLine("[AUTH] Signin endpoint called");
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.MapGet("/logout", async context =>
{
    Console.WriteLine("[AUTH] Logout endpoint called");
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

// Debug endpoint to reset authentication when MSAL issues occur
app.MapGet("/reset-auth", async context =>
{
    Console.WriteLine("[AUTH] Reset authentication called - clearing session and forcing fresh auth");
    context.Session.Clear();
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/setup-wod"
    });
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Improved token handler - uses direct token access from OIDC, bypassing MSAL account issues
internal class ApiAccessTokenHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string[] _scopes;

    public ApiAccessTokenHandler(
        ITokenAcquisition tokenAcquisition,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        _httpContextAccessor = httpContextAccessor;

        var configuredScopes = configuration.GetSection("AzureAd:ApiScopes").Get<string[]>() ?? Array.Empty<string>();
        if (configuredScopes.Length > 0)
        {
            _scopes = configuredScopes;
            return;
        }

        var singleScope = configuration["AzureAd:ApiScope"];
        _scopes = string.IsNullOrWhiteSpace(singleScope) ? Array.Empty<string>() : new[] { singleScope };
        
        Console.WriteLine($"[TOKEN HANDLER] Configured with scopes: {string.Join(", ", _scopes)}");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            Console.WriteLine($"[TOKEN] Skipping token acquisition - user not authenticated");
            return await base.SendAsync(request, cancellationToken);
        }

        Console.WriteLine($"[TOKEN] Acquiring token for user: {user.Identity.Name}");

        // PRIORITY 1: Try to get token directly from authentication properties (bypasses MSAL)
        // This works because we have SaveTokens = true in OIDC configuration
        if (httpContext != null)
        {
            var accessToken = await httpContext.GetTokenAsync("access_token");
            var expiresAt = await httpContext.GetTokenAsync("expires_at");
            
            Console.WriteLine($"[TOKEN] Direct token check - Access token present: {!string.IsNullOrEmpty(accessToken)}, Expires at: {expiresAt ?? "NOT SET"}");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                // Check if token is expired
                var isExpired = false;
                if (!string.IsNullOrEmpty(expiresAt) && DateTimeOffset.TryParse(expiresAt, out var expiry))
                {
                    isExpired = expiry <= DateTimeOffset.UtcNow;
                    Console.WriteLine($"[TOKEN] Token expiry: {expiry:u}, Now: {DateTimeOffset.UtcNow:u}, Expired: {isExpired}");
                }
                
                if (!isExpired)
                {
                    Console.WriteLine($"[TOKEN] SUCCESS: Using direct access token from auth properties (length: {accessToken.Length})");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    return await base.SendAsync(request, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"[TOKEN] Direct token expired - attempting refresh via MSAL");
                }
            }
            else
            {
                // Log all available tokens for debugging
                var idToken = await httpContext.GetTokenAsync("id_token");
                var refreshToken = await httpContext.GetTokenAsync("refresh_token");
                Console.WriteLine($"[TOKEN] Available tokens - id_token: {!string.IsNullOrEmpty(idToken)}, refresh_token: {!string.IsNullOrEmpty(refreshToken)}");
            }
        }

        // PRIORITY 2: Fall back to MSAL (may fail due to account association issue)
        try
        {
            Console.WriteLine($"[TOKEN] Attempting MSAL token acquisition with scopes: {string.Join(", ", _scopes)}");
            
            var accessTokenFromMsal = await _tokenAcquisition.GetAccessTokenForUserAsync(
                _scopes, 
                user: user,
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme);
            
            Console.WriteLine($"[TOKEN] SUCCESS: Token acquired via MSAL (length: {accessTokenFromMsal?.Length ?? 0})");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenFromMsal);
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            Console.WriteLine($"[TOKEN ERROR] MSAL challenge required: {ex.Message}");
            Console.WriteLine($"[TOKEN ERROR] MsalUiRequiredException - user needs to re-authenticate");
            
            // Mark that re-authentication is needed
            if (httpContext != null)
            {
                httpContext.Items["RequiresReauth"] = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TOKEN ERROR] MSAL token acquisition failed: {ex.GetType().Name} - {ex.Message}");
            
            // If MSAL fails, try one more time to use any available token
            if (httpContext != null)
            {
                var fallbackToken = await httpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(fallbackToken))
                {
                    Console.WriteLine($"[TOKEN] FALLBACK: Using access token despite MSAL failure (length: {fallbackToken.Length})");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fallbackToken);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}