using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using SideSeat.Data;
using SideSeat.Repositories;
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
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            var returnUrl = context.Request.Path + context.Request.QueryString;
                            var redirectUrl = $"/?auth=login&returnUrl={UrlEncoder.Default.Encode(returnUrl)}";
                            context.Response.Redirect(redirectUrl);
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            var returnUrl = context.Request.Path + context.Request.QueryString;
                            var redirectUrl = $"/Auth/AccessDenied?returnUrl={UrlEncoder.Default.Encode(returnUrl)}";
                            context.Response.Redirect(redirectUrl);
                            return Task.CompletedTask;
                        }
                    };
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
