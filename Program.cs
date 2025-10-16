using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

// Get DB connection environment variable
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrEmpty(connectionString))
    throw new Exception("DATABASE_URL is not set");

// Get server secret environment variable
var serverSecret = Environment.GetEnvironmentVariable("ZORAVAULT_SERVER_SECRET");
if (string.IsNullOrWhiteSpace(serverSecret))
    throw new Exception("ZORAVAULT_SERVER_SECRET is not set");

// EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

// Add services to the container.
builder.Services.AddScoped<AuthService>(provider =>
{
    var db = provider.GetRequiredService<ApplicationDbContext>();
    // var config = provider.GetRequiredService<IConfiguration>();
    return new AuthService(db, serverSecret);
});

// Add controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
