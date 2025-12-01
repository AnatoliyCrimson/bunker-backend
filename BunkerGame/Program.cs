using BunkerGame.Data;
using BunkerGame.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Добавлено
using Microsoft.IdentityModel.Tokens; // Добавлено
using System.Text;
using BunkerGame.Services; // Добавлено
using Microsoft.Extensions.Configuration; // Обычно уже импортировано
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Настройка Identity (оставляем как есть)
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- НАЧАЛО: Настройка JWT ---
// 1. Получаем секретный ключ из конфигурации
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["ValidIssuer"];
var audience = jwtSettings["ValidAudience"];

// Проверяем, что все необходимые настройки присутствуют
if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
{
    throw new InvalidOperationException("JwtSettings (SecretKey, ValidIssuer, ValidAudience) are not configured in appsettings.json");
}

// 2. Регистрируем сервис для работы с JWT (если понадобится в других сервисах)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// 3. Добавляем аутентификацию JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        // ClockSkew можно уменьшить для более строгой проверки (по умолчанию 5 минут)
        // ClockSkew = TimeSpan.Zero
    };
});
// --- КОНЕЦ: Настройка JWT ---

// --- НАЧАЛО: Настройка CORS ---
// Опционально: Замените "http://localhost:3000" на URL вашего React-приложения
// Или настройте более гибко через конфигурацию
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Замените на ваш URL клиента
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Обязательно, если используются credentials (cookies)
    });
});
// --- КОНЕЦ: Настройка CORS ---

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Bunker Game API", Version = "v1" });
    
    // Настройка безопасности для Swagger
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Введите токен в формате: Bearer {ваш_токен}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
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

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- НАЧАЛО: Middleware ---
app.UseRouting();

app.UseCors(policy =>
    policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Укажи порт твоего React-сервера
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader()); // Важно: вызывать до UseAuthentication и UseAuthorization

app.UseAuthentication(); // <-- Теперь добавлено и настроено
app.UseAuthorization();  // <-- Теперь добавлено и настроено
// --- КОНЕЦ: Middleware ---

app.MapControllers();

app.Run();

// --- НАЧАЛО: Вспомогательный класс для настройки JWT ---
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string ValidIssuer { get; set; } = string.Empty;
    public string ValidAudience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 час
    public int RefreshTokenExpirationDays { get; set; } = 7; // 7 дней
}
// --- КОНЕЦ: Вспомогательный класс для настройки JWT ---