using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

/**
Host associated
*/
using TemplateHost.Services.Security;
using Host.Services.Geo;
using Host.Services.Logging;

/**
Shared Common Objects library AppContractsSCO associated
*/
using AppContractsSCO.Services.Logging;
using AppContractsSCO.Services.Security;
using AppContractsSCO.Configuration;
using AppContractsSCO.Services.RealEstate;

//using AppContractsSCO.Services.RealEstate.ForSale;

/**
RCL associated
*/
using JuderemediosRCL.Infrastructure;
using RealEstateRCL.Infrastructure;



var builder = WebApplication.CreateBuilder(args);


// =====================================================
// CORE SERVICES
// =====================================================
builder.Services.AddSingleton<ISecureConfig, SecureConfig>();
builder.Services.AddSingleton<ISecureSettingsLoader, SecureSettingsLoader>();

//For GeoIP - begin
//We locate the large database external to the executable location
/*
builder.Services.AddSingleton<GeoLookupService>(sp =>
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "private", "GeoIP", "GeoLite2-City.mmdb");
    //Console.WriteLine($"dbPath: {dbPath}");
    return new GeoLookupService(dbPath);
});
*/
builder.Services.AddSingleton<GeoLookupService>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var dbPath = Path.Combine(
        env.ContentRootPath,
        "private",
        "GeoIP",
        "GeoLite2-City.mmdb");

    Console.WriteLine($"dbPath: {dbPath}");

    return new GeoLookupService(dbPath);
});
//For GeoIP - end


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IPageHitService, PageHitService>();

//builder.Services.AddSingleton<IForSaleSettingsService>();
builder.Services.AddScoped<IForSaleSettingsService, ForSaleSettingsService>();


// =====================
// DATA PROTECTION (ONLY ONCE)
// =====================
/*
builder.Services.AddDataProtection()
    .SetApplicationName("TemplateHost")
    .PersistKeysToFileSystem(new DirectoryInfo("/home/ken/.aspnet-keys-template"));
*/

var keyPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".aspnet-keys");

Directory.CreateDirectory(keyPath);

keyPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".aspnet-keys-template");

Directory.CreateDirectory(keyPath);

builder.Services.AddDataProtection()
    .SetApplicationName("TemplateHost")
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));   

// =====================
// AUTH (ONLY ONE SCHEME)
// =====================
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "TemplateHost.Auth";
        options.LoginPath = "/Juderemedios/AdminAuth/Login";
        options.LogoutPath = "/logout";

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                var path = context.Request.Path.Value ?? "";

                string module = "general";

                if (path.Contains("/RealEstate"))
                    module = "keswick";
                else if (path.Contains("/Juderemedios"))
                    module = "juderemedios";

                var returnUrl = context.Request.Path + context.Request.QueryString;

                context.Response.Redirect(
                    $"/AdminAuth/Login?module={module}&returnUrl={returnUrl}");

                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();

// =====================
// MVC + RCL
// =====================
/*
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(JuderemediosRCL.Areas.Juderemedios.Controllers.AdminAuthController).Assembly);
*/
/*
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(JuderemediosRCL.Areas.Juderemedios.Controllers.AdminAuthController).Assembly)
    .AddApplicationPart(typeof(RealEstateRCL.Areas.RealEstate.Controllers.HomeController).Assembly);
*/
/**
RCL associated
*/
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(JuderemediosRCLMarker).Assembly)
    .AddApplicationPart(typeof(RealEstateRCLMarker).Assembly);


var app = builder.Build();

// =====================
// PIPELINE (MINIMAL)
// =====================
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// =====================
// DEBUG: COOKIE + AUTH STATE
// =====================
app.Use(async (ctx, next) =>
{
    Console.WriteLine("=== REQUEST ===");
    Console.WriteLine("Path: " + ctx.Request.Path);
    Console.WriteLine("Cookie: " + ctx.Request.Headers.Cookie);
    Console.WriteLine("User Authenticated: " + ctx.User.Identity?.IsAuthenticated);
    await next();
});



// =====================
// REQUIRED for images contained within an RCL to work
// =====================
app.UseStaticFiles();



// =====================
// ROUTES
// =====================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();