using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Call DatabaseInitializer with IConfiguration
new DatabaseInitializer(builder.Configuration).EnsureTablesExist();

// ✅ Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Add MVC and HttpContext access
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ✅ Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
});

// Optional: Allow injecting IConfiguration if needed elsewhere
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Build the app
var app = builder.Build();

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

app.UseSession();         // Session middleware
app.UseAuthentication();  // Auth middleware
app.UseAuthorization();   // Authorization

// ✅ Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
