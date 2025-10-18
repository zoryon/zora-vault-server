using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Extensions;
using ZoraVault.Middleware;
using ZoraVault.Services;
using ZoraVault.Configuration;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var dbConnection = builder.Configuration["DATABASE_URL"]
                   ?? throw new Exception("DATABASE_URL is not set");

// EF Core DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(dbConnection, new MySqlServerVersion(new Version(8, 0, 33))));

builder.Services.AddAppSecrets(builder.Configuration, validateOnStart: false);

// Add services to the container.
builder.Services.AddScoped<AuthService>(sp =>
{
    var db = sp.GetRequiredService<ApplicationDbContext>();
    var secrets = sp.GetRequiredService<Secrets>();
    return new AuthService(
        db,
        serverSecret: secrets.ServerSecret,
        refreshTokenSecret: secrets.RefreshTokenSecret,
        accessTokenSecret: secrets.AccessTokenSecret,
        challengesApiSecret: secrets.ChallengesApiSecret,
        sessionApiSecret: secrets.SessionApiSecret
    );
});

builder.Services.AddScoped<DeviceService>(sp =>
{
    var db = sp.GetRequiredService<ApplicationDbContext>();
    var secrets = sp.GetRequiredService<Secrets>();
    return new DeviceService(db, sessionApiSecret: secrets.SessionApiSecret);
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

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
