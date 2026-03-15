using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Application.Services;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Infrastructure.Data;
using WebApiRbac.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// retrieve the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// registering ApplicationDbContext to the system with the PostgreSQL driver
builder.Services.AddDbContext<ApplicationDbContext> (options => options.UseNpgsql(connectionString));

// Mendaftarkan Repository (Infrastructure)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// authentication
builder.Services.AddAuthentication(options =>
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key missing"))
        ),

        // validasi penerbit (siapa yg membuat token ini)
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        // validasi target (untuk aplikasi apa token ini?
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // validasi umur (apakah sudah expired?)
        ValidateLifetime = true,

        // Secara default, .NET memberi "toleransi waktu" (Clock Skew) selama 5 menit untuk token yang expired.
        // Karena kita sudah punya sistem Refresh Token yang canggih, kita tidak butuh toleransi itu.
        // Kita atur ke 0 agar token mati TEPAT pada detiknya!
        ClockSkew = TimeSpan.Zero
    };
});

// Menambahkan fitur Authorization (Mengecek Hak Akses / Role)
builder.Services.AddAuthorization();

// Add services to the container
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // jwt
app.UseAuthorization(); // permission

app.MapControllers();

app.Run();
