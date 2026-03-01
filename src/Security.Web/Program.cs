using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Security.Application;
using Security.Infrastructure.Data;
using Security.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Application + Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// IdentityOptions — explicit, reviewable, and testable.
// These settings override the defaults set in AddInfrastructureServices.
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password policy: 10+ chars, uppercase + lowercase + digit + symbol required.
    options.Password.RequiredLength = 10;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    // Account lockout: 5 consecutive failures → 15-minute lockout.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
});

// MVC + Razor
builder.Services.AddControllersWithViews(options =>
{
    // Global anti-forgery filter applied to all POST/PUT/PATCH/DELETE actions
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminArea", policy =>
        policy.RequireRole("SuperAdmin", "Admin"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // Default sliding window for regular (non-remembered) sessions.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "Security.Auth";
    // Persistent "remember me" cookies get a 14-day sliding window;
    // non-persistent session cookies keep the 30-minute default above.
    options.Events.OnSigningIn = ctx =>
    {
        if (ctx.Properties.IsPersistent)
        {
            ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);
        }
        return Task.CompletedTask;
    };
});

// JWT Bearer authentication for API endpoints.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured in 'Jwt:Key'.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("'Jwt:Key' must be at least 32 characters for HMAC-SHA256.");
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "SecurityApp",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "SecurityApi",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "Security.Session";
});

// HSTS: 1-year max-age, include subdomains, opt-in to preload list.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

// Deterministic startup: create DB → run scripts → seed roles + SuperAdmin.
// Must run before app.Run() so the schema is ready before the first request.
try
{
    await using var scope = app.Services.CreateAsyncScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IDatabaseBootstrapper>();
    await bootstrapper.InitializeAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database initialisation failed – ensure a SQL Server connection string is configured.");
    throw;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS: 1-year max-age, include subdomains, opt-in to preload list.
    app.UseHsts();
}

// Security response headers applied to every response.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // NOTE: 'unsafe-inline' is included to support existing Razor inline scripts/styles.
    // Tighten to nonce-based CSP once inline code is refactored into external files.
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// API routes (attribute-routed controllers under /api)
app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .RequireAuthorization("AdminArea");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
