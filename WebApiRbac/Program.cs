using Scalar.AspNetCore;
using WebApiRbac.Extensions;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureDependencies();
builder.Services.ConfigureJwtAuthentication(builder.Configuration);

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

await app.SeedDatabaseAsync();

app.Run();
