using Microsoft.EntityFrameworkCore;
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

// Add services to the container
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
