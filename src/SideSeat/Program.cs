using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Repositories;

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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

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
