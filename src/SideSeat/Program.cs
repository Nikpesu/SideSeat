using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using SideSeat.Data;
using SideSeat.Hubs;
using SideSeat.Models;
using SideSeat.Middleware;
using SideSeat.Repositories;
using SideSeat.Security;
using SideSeat.Services;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;

namespace SideSeat
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                options.UseUtcTimestamp = true;
            });
            var connectionString = builder.Configuration.GetConnectionString("SideSeatDbContext");
            if (builder.Environment.IsEnvironment("Docker") &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__SideSeatDbContext")))
            {
                connectionString = new SqlConnectionStringBuilder
                {
                    DataSource = "sideseat-db,1433",
                    InitialCatalog = "SideSeat",
                    UserID = "sa",
                    Password = builder.Configuration["SA_PASSWORD"] ?? "SideSeat123!",
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true
                }.ConnectionString;
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<SideSeatDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddScoped<SideSeatEfRepository>();
            builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAiContextService, AiContextService>();
            builder.Services.AddScoped<IAiToolService, AiToolService>();
            builder.Services.AddScoped<ISideSeatCommandService, SideSeatCommandService>();
            builder.Services.AddScoped<IPendingActionService, PendingActionService>();
            builder.Services.AddScoped<IAuditService, AuditService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.Configure<MapsOptions>(
                builder.Configuration.GetSection(MapsOptions.SectionName));
            builder.Services.Configure<PublicWebSearchOptions>(
                builder.Configuration.GetSection(PublicWebSearchOptions.SectionName));
            builder.Services.AddHttpClient("Nominatim", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddHttpClient("Routing", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(2);
            });
            builder.Services.AddHttpClient("PublicWebSearch", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            builder.Services.AddSingleton<ICityGeocodingService, NominatimCityGeocodingService>();
            builder.Services.AddSingleton<IRouteGeometryService, OsrmRouteGeometryService>();
            builder.Services.AddSingleton<IPublicWebSearchService, PublicWebSearchService>();
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["correlationId"] =
                        context.HttpContext.TraceIdentifier;
                };
            });
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddSignalR();
            builder.Services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();
            builder.Services.Configure<OpenWebUiOptions>(
                builder.Configuration.GetSection(OpenWebUiOptions.SectionName));
            builder.Services.AddHttpClient<IOpenWebUiService, OpenWebUiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(90);
            });
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("ai", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 12,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 2,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
                options.AddPolicy("maps", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 120,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
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
            var authentication = builder.Services.AddAuthentication();
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authentication.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }
            builder.Services.AddAuthorization();

            var app = builder.Build();
            var useDummyData = builder.Configuration.GetValue<bool>("DUMMY_DATA");

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
                app.UseExceptionHandler();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!app.Environment.IsEnvironment("Docker"))
            {
                app.UseHttpsRedirection();
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<RequestObservabilityMiddleware>();
            app.UseMiddleware<McpApiKeyMiddleware>();
            app.UseRateLimiter();
            app.UseStatusCodePagesWithReExecute("/Home/HttpStatus/{0}");

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
                if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
                {
                    var logger = scope.ServiceProvider
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("DatabaseMigration");
                    for (var attempt = 1; attempt <= 6; attempt++)
                    {
                        try
                        {
                            await dbContext.Database.MigrateAsync();
                            break;
                        }
                        catch (Exception exception) when (attempt < 6)
                        {
                            logger.LogWarning(
                                exception,
                                "Database migration attempt {Attempt} failed. Retrying.",
                                attempt);
                            await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
                        }
                    }
                }

                if (app.Environment.IsEnvironment("Docker") && !useDummyData)
                {
                    await DummyDataCleaner.RemoveAsync(dbContext);
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
            app.MapHealthChecks("/health/live", new()
            {
                Predicate = _ => false
            });
            app.MapHealthChecks("/health/ready", new()
            {
                Predicate = registration => registration.Tags.Contains("ready")
            });
            app.MapHub<RideHub>("/hubs/rides");
            app.MapMcp("/mcp");

            if (useDummyData)
            {
                await Models.Lab1Demo.RunAsync();
            }

            app.Run();
        }
    }
}
