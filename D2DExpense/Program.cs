using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Configure session (optional, for other use)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Add MVC and HttpContext access
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ✅ Configure persistent cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "UserAuthCookie"; // Optional custom name
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Only over HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; // Allows cross-tab login
        options.LoginPath = "/Account/Login";     // Redirect here if unauthenticated
        options.LogoutPath = "/Account/Logout";   // Optional logout path
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // ✅ Cookie lasts 30 days
        options.SlidingExpiration = true;              // ⏳ Refresh on each request
    });

// Optional: Allow injecting IConfiguration if needed elsewhere
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddHttpClient<INAVService, NAVService>();


// ✅ Build the app
var app = builder.Build();

// ✅ Initialize database safely *after* app is built
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    new DatabaseInitializer(config).EnsureTablesExist();
}

// ✅ Environment-based error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ✅ Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();         // Session middleware (optional, not used for login persistence)
app.UseAuthentication();  // 🔐 Auth middleware (must come before Authorization)
app.UseAuthorization();   // 🔐 Authorization middleware

// ✅ Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
