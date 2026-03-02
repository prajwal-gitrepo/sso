// Import required namespaces for authentication, authorization, and minimal API
using Microsoft.AspNetCore.Authentication;// Need for signout functionality
using Microsoft.AspNetCore.Authentication.Cookies; // Cookie authentication
using Microsoft.AspNetCore.Authentication.OpenIdConnect; // OpenID Connect authentication
using Microsoft.AspNetCore.Authorization; // Authorization policies
using System.Security.Claims;

// Create a WebApplication builder with command-line args
var builder = WebApplication.CreateBuilder(args);

// Register Swagger services for API documentation
builder.Services.AddEndpointsApiExplorer(); // Enables endpoint discovery for Swagger
builder.Services.AddSwaggerGen(); // Adds Swagger generator

// Configure authentication with cookies and OpenID Connect (Azure AD)
builder.Services.AddAuthentication(options =>
{
    // Set default authentication and challenge schemes
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
// Add cookie authentication handler
.AddCookie()
// Add OpenID Connect authentication handler
.AddOpenIdConnect(options =>
{
    // Bind AzureAd settings from configuration (appsettings.json)
    builder.Configuration.Bind("AzureAd", options);
    // Set the authority (issuer) for Azure AD
    options.Authority = $"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/v2.0";
    // Configure event to capture the raw token after authentication
    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = async context =>
        {
            var jwtToken = context.SecurityToken;
            if (jwtToken != null)
            {
                // Add the raw access token as a claim for later retrieval
                var accessToken = jwtToken.RawData;
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity != null && !identity.HasClaim(c => c.Type == "access_token"))
                {
                    identity.AddClaim(new Claim("access_token", accessToken));
                }
                // Log token details to the console (for debugging)
                var issuer = jwtToken.Issuer;
                var audience = jwtToken.Audiences.FirstOrDefault();
                var claims = jwtToken.Claims;
                Console.WriteLine($"Token Issuer: {issuer}");
                Console.WriteLine($"Token Audience: {audience}");
                Console.WriteLine($"Token Claims: {string.Join(", ", claims.Select(c => $"{c.Type}:{c.Value}"))}");
            }
            await Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
        .Build());

// Build the WebApplication
var app = builder.Build();

// Public endpoint: redirect root to Swagger UI
app.MapGet("/", (HttpContext context) =>
{
    context.Response.Redirect("/swagger"); // Redirect to Swagger UI
    return Task.CompletedTask;
}).AllowAnonymous(); // Allow anonymous access to the root endpoint
// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // Enable HTTP Strict Transport Security
}


app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseRouting(); // Enable endpoint routing

app.UseStaticFiles(); // Enable serving static files from wwwroot

app.UseAuthentication();    // Enable authentication middleware
app.UseAuthorization();     // Enable authorization middleware

// Enable Swagger middleware for API documentation
app.UseSwagger(); // Serve Swagger JSON endpoint
app.UseSwaggerUI(options =>
{
    options.InjectJavascript("/swagger-signout.js"); // Inject custom JS for sign out button
}); // Serve Swagger UI

// Protected endpoint: return the raw access token from claims
app.MapGet("/GetToken", (HttpContext context) =>
{
    // Retrieve the access token from the user's claims
    var token = context.User.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
    return token ?? "No access token claim available.";
})
.WithTags("Get-Token");

// Sign out endpoint: clears authentication cookies and redirects to sign-in page
app.MapGet("/signout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });

    return Results.Empty;
})
.ExcludeFromDescription();

// Start the application
app.Run();