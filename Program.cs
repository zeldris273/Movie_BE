using Amazon.S3;
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.IISIntegration;

var builder = WebApplication.CreateBuilder(args);

// Tăng giới hạn kích thước request body (500MB)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1024 * 1024 * 500; // 500MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 500; // 500MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Tăng timeout lên 10 phút
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

// Thêm dịch vụ controllers
builder.Services.AddControllers();

// Thêm Swagger
builder.Services.AddSwaggerGen();

// Đăng ký AuthService
builder.Services.AddScoped<AuthService>();

// Đăng ký S3Service và IAmazonS3
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.APNortheast1 // ap-northeast-1 (Tokyo)
    };
    return new AmazonS3Client(
        builder.Configuration["AWS:AccessKey"],
        builder.Configuration["AWS:SecretKey"],
        s3Config);
});

builder.Services.AddScoped<S3Service>();

// Đăng ký DbContext
builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 29))));

// Cấu hình Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Movie API", Version = "v1" });
    c.OperationFilter<SwaggerFileUploadOperationFilter>();
});

// Thêm MemoryCache
builder.Services.AddMemoryCache();

// Thêm cấu hình xác thực JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
app.MapControllers();

app.Run();