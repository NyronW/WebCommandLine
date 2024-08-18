using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using WebApp.Authorization;
using WebApp.MvcDemo.Services;
using WebCommandLine;
using WebCommandLine.Commands;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSingleton<InMemoryUserStore>();

builder.Services.AddWebCommandLine(options =>
{
    options.StaticFilesUrl = "/MyWebAssets"; //This will be the base path for static files
    options.WebCliUrl = "/MyWebCli"; // cammand requests will goes to this endpoint
    // If true the JavaScript bjects with be automatically initialized, otherwise you have to manually inti window.cli object
    // You would typically set this value to false when you want to override the default httpHandler
    options.AutoInitJsInstance = false; 
}, typeof(ShowTable).Assembly);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddTransient<IAuthorizationHandler, WebCmdLineAuthHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminUser", policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.RequireClaim("isAdmin", "true");
    });

    options.AddPolicy("PowerUser", policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
        //policyBuilder.AddRequirements(new WebCmdLineRequirement());
        policyBuilder.RequireAssertion(ctx =>
        {
            return ctx.User.IsInRole("BusinessAdmin");
        });
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseWebCommandLine();

app.Run();
