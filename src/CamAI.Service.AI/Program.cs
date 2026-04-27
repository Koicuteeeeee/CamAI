using CamAI.Service.AI.BLL.Interfaces;
using CamAI.Service.AI.BLL.Services;
using CamAI.Service.AI.DAL.Interfaces;
using CamAI.Service.AI.DAL.Repositories;
using CamAI.Service.AI.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// === CẤU HÌNH CONFIGURATION ===
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath);
builder.Configuration.AddJsonFile("API/appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"API/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// === CẤU HÌNH SERVICES ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === AUTHENTICATION SERVICES ===
builder.Services.AddHttpClient<IKeycloakAuthService, KeycloakAuthService>();
builder.Services.AddTransient<AuthHeaderHandler>();

// Connection
builder.Services.AddHttpClient("CamAI_API", client =>
{
    client.BaseAddress = new Uri("http://localhost:5282/"); // Port của CamAI.API
})
.AddHttpMessageHandler<AuthHeaderHandler>();

// DAL - Repositories
builder.Services.AddSingleton<ICameraRepository, CameraRepository>();
builder.Services.AddSingleton<IFaceDataRepository, FaceDataRepository>();
builder.Services.AddSingleton<IApiLogRepository, ApiLogRepository>();

// Đường dẫn tới thư mục chứa model ONNX
var modelsPath = Path.Combine(builder.Environment.ContentRootPath, "Infrastructure", "Models", "onnx");
Directory.CreateDirectory(modelsPath);

// Đăng ký AI Services vào DI Container
builder.Services.AddSingleton<IFaceDetector>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<YuNetFaceDetector>>();
    var modelFile = Path.Combine(modelsPath, "yunet.onnx");
    return new YuNetFaceDetector(modelFile, logger);
});

builder.Services.AddSingleton<IFaceEmbedder>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SFaceEmbedder>>();
    var modelFile = Path.Combine(modelsPath, "sface.onnx");
    return new SFaceEmbedder(modelFile, logger);
});

builder.Services.AddSingleton<IFaceMatchService, ApiFaceMatchService>();

// Thêm dịch vụ đăng ký từ stream
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

// Thêm dịch vụ ghi log camera
builder.Services.AddSingleton<ICameraEventLogger, CameraEventLogger>();

// Thêm dịch vụ trung chuyển hình ảnh
builder.Services.AddSingleton<IStreamProvider, StreamProvider>();

// Thêm MinIO Storage Service
builder.Services.AddSingleton<IMinioStorageService, MinioStorageService>();

// Kích hoạt con mắt AI quét luồng stream
builder.Services.AddHostedService<CameraService>();

// CORS - cho phép Web App gọi API
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

// === CẤU HÌNH PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => new
{
    service = "CamAI.Service.AI",
    status = "running",
    version = "1.0.0",
    endpoints = new[]
    {
        "POST /api/face/recognize - Nhận diện khuôn mặt từ ảnh",
        "POST /api/face/register - Đăng ký khuôn mặt mới",
        "GET  /api/face/registered - Xem danh sách đã đăng ký",
        "DELETE /api/face/{userId} - Xóa người dùng"
    }
});

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("  CamAI - AI Engine Service");
app.Logger.LogInformation("  Models path: {Path}", modelsPath);
app.Logger.LogInformation("===========================================");

try
{
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "APP CRASHED! Nguyen nhan: {Message}", ex.Message);
    throw;
}
