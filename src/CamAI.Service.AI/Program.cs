using CamAI.Common.Interfaces;
using CamAI.Service.AI.Services;
using CamAI.Service.AI.BLL.Services;

var builder = WebApplication.CreateBuilder(args);

// === CẤU HÌNH SERVICES ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đường dẫn tới thư mục chứa model ONNX
var modelsPath = Path.Combine(builder.Environment.ContentRootPath, "Infrastructure", "Models", "onnx");

// Tạo thư mục nếu chưa có
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

builder.Services.AddHttpClient("CamAI_API", client =>
{
    client.BaseAddress = new Uri("http://localhost:5282/"); // Port của CamAI.API
});

builder.Services.AddSingleton<IFaceMatchService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("CamAI_API");
    var logger = sp.GetRequiredService<ILogger<ApiFaceMatchService>>();
    return new ApiFaceMatchService(client, logger);
});

// Thêm dịch vụ trung chuyển hình ảnh
builder.Services.AddSingleton<StreamProvider>();

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
