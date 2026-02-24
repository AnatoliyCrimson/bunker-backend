using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WorkflowCore.Interface;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка подключения к БД (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'Default' not found.");

Console.WriteLine($"---> ИСПОЛЬЗУЕМАЯ СТРОКА ПОДКЛЮЧЕНИЯ: {connectionString}");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson(); // <-- Вот эта команда решает твою ошибку
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource));

// 2. Настройка Redis (Кэш)
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
builder.Services.AddWorkflow();

// 6. SignalR
builder.Services.AddSignalR()
       .AddStackExchangeRedis(redisConnection, options => {
           options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("BunkerGame_SignalR");
       });

// 7. РЕГИСТРАЦИЯ СЕРВИСОВ
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>(); 

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 8. Настройка Swagger с поддержкой JWT
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Bunker Game API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Введите токен",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>(); // Твой класс контекста
        context.Database.Migrate();
        Console.WriteLine("--> Миграции успешно применены.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Ошибка при применении миграций: {ex.Message}");
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); 

app.UseRouting();

app.UseCors(x => x
    .WithOrigins("http://localhost:3000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// --- Вспомогательный класс ---
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string ValidIssuer { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7; 
}