using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.Services;
using BunkerGame.Workflows; // <-- Должна существовать папка Workflows с классами
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkflowCore.Interface; // <-- Требует dotnet add package WorkflowCore

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка подключения к БД
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Настройка Redis (Кэш)
// Требует: dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConnection;
    options.InstanceName = "BunkerGame_";
});

// 3. Настройка Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 4. Настройка JWT
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
var secretKey = jwtSettings?.SecretKey;

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JwtSettings:SecretKey is missing in appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings?.ValidIssuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings?.ValidAudience,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Workflow Core
// Требует: dotnet add package WorkflowCore.Persistence.PostgreSQL
builder.Services.AddWorkflow(x => x.UsePostgreSQL(connectionString, true, true));

// 6. SignalR
// Требует: dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options => {
        // Исправляем Warning CS0618, явно указывая тип канала
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("BunkerGame_SignalR");
    });

// 7. Регистрация сервисов приложения
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Запуск хоста Workflow и регистрация схемы
var host = app.Services.GetService<IWorkflowHost>();
if (host != null)
{
    // Эти классы (GameWorkflow, GameData) должны быть созданы в папке Workflows
    host.RegisterWorkflow<GameWorkflow, GameData>();
    host.Start();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// CORS должен быть до Auth
app.UseCors(x => x
    .WithOrigins("http://localhost:3000") // Укажи свой URL фронтенда
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// app.MapHub<GameHub>("/gameHub"); // Раскомментируем, когда создадим хаб

app.Run();

// --- Вспомогательный класс настроек JWT (как ты и просил, внутри файла) ---
namespace BunkerGame.Models // Namespace должен совпадать с тем, что ожидает TokenService
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string ValidIssuer { get; set; } = string.Empty;
        public string ValidAudience { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}