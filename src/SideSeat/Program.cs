using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Repositories;
using SideSeat.Security;
using SideSeat.Services;
using System.Globalization;
using System.Text.Encodings.Web;

namespace SideSeat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<SideSeatDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SideSeatDbContext")));
            builder.Services.AddScoped<SideSeatEfRepository>();
            builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
            builder.Services
                .AddIdentity<AppUser, IdentityRole<int>>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;
                })
                .AddEntityFrameworkStores<SideSeatDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, SideSeatUserClaimsPrincipalFactory>();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var redirectUrl = $"/?auth=login&returnUrl={UrlEncoder.Default.Encode(returnUrl)}";
                    context.Response.Redirect(redirectUrl);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var redirectUrl = $"/Auth/AccessDenied?returnUrl={UrlEncoder.Default.Encode(returnUrl)}";
                    context.Response.Redirect(redirectUrl);
                    return Task.CompletedTask;
                };
            });
            builder.Services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            var supportedCultures = new[]
            {
                new CultureInfo("hr-HR"),
                new CultureInfo("en-US")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("hr-HR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStatusCodePagesWithReExecute("/Home/HttpStatus/{0}");

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
                if (app.Environment.IsEnvironment("Docker"))
                {
                    await dbContext.Database.MigrateAsync();
                }

                await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
            }

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "gradovi-list",
                pattern: "gradovi",
                defaults: new { controller = "Grad", action = "Index" });

            app.MapControllerRoute(
                name: "gradovi-detalji",
                pattern: "gradovi/{id:int}",
                defaults: new { controller = "Grad", action = "Details" });

            app.MapControllerRoute(
                name: "voznje-aktivne",
                pattern: "voznje/aktivne",
                defaults: new { controller = "Voznja", action = "Active" });

            app.MapControllerRoute(
                name: "korisnici-profil",
                pattern: "korisnici/{id:int}/profil",
                defaults: new { controller = "Korisnik", action = "Details" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            await Models.Lab1Demo.RunAsync();

            app.Run();
        }
    }
}
