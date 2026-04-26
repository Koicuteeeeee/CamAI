using CamAI.API.BLL.Interfaces;
using CamAI.API.BLL.Services;
using CamAI.API.DAL.Repository;

var builder = WebApplication.CreateBuilder(args);

// === SERVICES ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' chưa được cấu hình.");

// DAL - Repository
builder.Services.AddScoped(_ => new UserRepository(connectionString));
builder.Services.AddScoped(_ => new AccessLogRepository(connectionString));
builder.Services.AddScoped(_ => new CameraRepository(connectionString));

// BLL - Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccessLogService, AccessLogService>();
builder.Services.AddScoped<ICameraService, CameraService>();

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
    app.UseSwaggerUI();
}

app.UseCors();
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
