using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Services;
using GestionQ.Web.Security;
using GestionQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GestionQ.Domain.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.WebHost.UseUrls("http://*:5144");
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IElectronicInvoicingService, ArcaElectronicInvoicingService>();
builder.Services.AddScoped<IScaleService, KretzJDataGateService>();
builder.Services.AddHttpClient<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();
builder.Services.AddScoped<ICloudStorageService, FtpStorageService>();
builder.Services.AddScoped<GestionQ.Web.Services.IArcaAutomationService, GestionQ.Web.Services.ArcaAutomationService>();
builder.Services.AddHostedService<GestionQ.Web.Services.DatabaseInitializationService>();
builder.Services.AddHostedService<GestionQ.Web.Services.NgrokTunnelService>();
builder.Services.AddHostedService<GestionQ.Web.Services.StatsSyncService>();
builder.Services.AddHostedService<GestionQ.Web.Services.ArcaRetryBackgroundService>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Allow spaces and additional characters in usernames
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GestionQ_ErrorLog.txt");
        System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: {ex.ToString()}{Environment.NewLine}");
        throw;
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var cultureInfo = new System.Globalization.CultureInfo("es-AR");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
cultureInfo.NumberFormat.NumberGroupSeparator = ",";
cultureInfo.NumberFormat.CurrencyGroupSeparator = ",";

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new List<System.Globalization.CultureInfo> { cultureInfo },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { cultureInfo }
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GestionQ.Web.Middleware.LicenseMiddleware>();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
