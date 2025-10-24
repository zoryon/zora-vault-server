using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using ZoraVault.Data;
using ZoraVault.Extensions;
using ZoraVault.Middlewares;
using ZoraVault.Services;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------------
// CONFIGURATION
// --------------------------------------------------------------------
builder.Configuration.AddEnvironmentVariables();

var dbConnection = builder.Configuration["DATABASE_URL"]
                   ?? throw new Exception("DATABASE_URL is not set");

// --------------------------------------------------------------------
// DATABASE
// --------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(dbConnection, new MySqlServerVersion(new Version(8, 0, 33))));

// --------------------------------------------------------------------
// SECRETS
// --------------------------------------------------------------------
builder.Services.AddAppSecrets(builder.Configuration);

// --------------------------------------------------------------------
// SERVICES
// --------------------------------------------------------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DeviceService>();

// --------------------------------------------------------------------
// CONTROLLERS & SWAGGER
// --------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --------------------------------------------------------------------
// SMTP EMAIL SERVICE
// --------------------------------------------------------------------
builder.Services.AddTransient<IEmailService, EmailService>();

// --------------------------------------------------------------------
// MIDDLEWARE PIPELINE
// --------------------------------------------------------------------
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// GLOBAL CUSTOM MIDDLEWARES (in order)
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
