using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using KanbanBoardAPI.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ========== КОНФИГУРАЦИЯ СЕРВИСОВ ========== //

// Добавление сервисов для Razor Pages и API контроллеров
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Настройка сериализации JSON для обработки циклических ссылок
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// Настройка подключения к базе данных SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== КОНФИГУРАЦИЯ АУТЕНТИФИКАЦИИ ========== //

// Получение настроек JWT из конфигурации
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

// Настройка JWT аутентификации
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Точное время без допуска
    };
});

// ========== КОНФИГУРАЦИЯ SWAGGER ========== //

// Настройка документации API с помощью Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Kanban Board API", Version = "v1" });

    // Добавление поддержки JWT в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========== НАСТРОЙКА CORS ========== //

// Разрешение всех источников, методов и заголовков (для разработки)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========== КОНФИГУРАЦИЯ MIDDLEWARE ========== //

// В development режиме показываем подробные ошибки и Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kanban Board API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// Перенаправление на HTTPS и обработка статических файлов
app.UseHttpsRedirection();
app.UseStaticFiles();

// Включение маршрутизации и CORS
app.UseRouting();
app.UseCors("AllowAll");

// Включение аутентификации и авторизации
app.UseAuthentication();
app.UseAuthorization();

// Настройка конечных точек
app.MapRazorPages();
app.MapControllers();

// ========== ПРИМЕНЕНИЕ МИГРАЦИЙ БАЗЫ ДАННЫХ ========== //

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync(); // Автоматическое применение миграций
}

// Запуск приложения
app.Run();