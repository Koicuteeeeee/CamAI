using CamAI.API.BLL.Interfaces;
using CamAI.API.BLL.Services;
using CamAI.API.DAL.Interfaces;
using CamAI.API.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// === CẤU HÌNH CONFIGURATION ===
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath);
builder.Configuration.AddJsonFile("API/appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"API/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// === SERVICES ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CamAI API", Version = "v1" });
    
    // Cấu hình OAuth2 cho Swagger
    c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["Keycloak:Authority"]}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{builder.Configuration["Keycloak:Authority"]}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User Profile" }
                }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" }
            },
            new[] { "openid", "profile" }
        }
    });
});

// === AUTHENTICATION & AUTHORIZATION ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true, // Bật lại kiểm tra Audience để đảm bảo bảo mật chặt chẽ
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

// Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' chưa được cấu hình.");

// DAL - Repository
builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(connectionString));
builder.Services.AddScoped<IFaceProfileRepository>(_ => new FaceProfileRepository(connectionString));
builder.Services.AddScoped<IAccessLogRepository>(_ => new AccessLogRepository(connectionString));
builder.Services.AddScoped<ICameraRepository>(_ => new CameraRepository(connectionString));
builder.Services.AddScoped<ICameraEventRepository>(_ => new CameraEventRepository(connectionString));

// BLL - Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFaceProfileService, FaceProfileService>();
builder.Services.AddScoped<IAccessLogService, AccessLogService>();
builder.Services.AddScoped<ICameraService, CameraService>();
builder.Services.AddScoped<ICameraEventService, CameraEventService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// === PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CamAI API v1");
        c.OAuthClientId("camai-api");
        c.OAuthUsePkce(); // Khuyên dùng cho Authorization Code flow
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/", () => new
{
    service = "CamAI.API",
    status = "running",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET    /api/users           - Danh sách User",
        "GET    /api/users/{id}      - Chi tiết User",
        "GET    /api/users/faces     - Tất cả Face Embeddings",
        "DELETE /api/users/{id}      - Xóa User",
        "GET    /api/accesslogs      - Lịch sử truy cập",
        "POST   /api/accesslogs      - Ghi nhật ký mới"
    }
});

app.Run();
