using Microsoft.AspNetCore.Authorization;
using Security.Application;
using Security.Infrastructure.Data;
using Security.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Application + Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "Security.Auth";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "Security.Session";
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .RequireAuthorization("AdminArea");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
