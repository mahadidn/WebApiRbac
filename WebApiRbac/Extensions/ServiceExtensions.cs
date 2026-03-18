using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Application.Services;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Infrastructure.BackgroundJobs;
using WebApiRbac.Infrastructure.Data;
using WebApiRbac.Infrastructure.Repositories;
using WebApiRbac.Infrastructure.Security;

namespace WebApiRbac.Extensions
{
    public static class ServiceExtensions
    {

        // konfigurasi database
        public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // retrieve the connection string from appsettings.json
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            // registering ApplicationDbContext to the system with the PostgreSQL driver
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        }

        // dependency injection configuration (repository & service)
        public static void ConfigureDependencies(this IServiceCollection services)
        {
            // Mendaftarkan Repository (Infrastructure)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserService, UserService>();
            services.AddHostedService<TokenCleanupService>();
        }

        // JWT & Auth Configuration
        public static void ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // authentication
            services.AddAuthentication(options =>
            {
                // mengatur bahwa aplikasi kita menggunakan pola "Bearer Token" sebagai standar
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {

                    // validasi signature
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new Exception("JWT Key missing"))
                    ),

                    // validasi penerbit (siapa yg membuat token ini)
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],

                    // validasi target (untuk aplikasi apa token ini?
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],

                    // validasi umur (apakah sudah expired?)
                    ValidateLifetime = true,

                    // Secara default, .NET memberi "toleransi waktu" (Clock Skew) selama 5 menit untuk token yang expired.
                    // Karena sudah ada sistem Refresh Token, kita tidak butuh toleransi itu.
                    // Atur ke 0 agar token mati TEPAT pada detiknya!
                    ClockSkew = TimeSpan.Zero,

                    RoleClaimType = "role",
                };
            });

            // Menambahkan fitur Authorization (Mengecek Hak Akses / Role)
            services.AddAuthorization();

            // daftarkan aturan dinamis (hunakan singleton karena ini aturan baku)
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            // daftarkan security
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        }

    }
}
