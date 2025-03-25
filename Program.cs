using Amazon.S3;
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ controllers
builder.Services.AddControllers();

// Thêm Swagger
builder.Services.AddSwaggerGen();

// Đăng ký AuthService (chỉ cần một lần, loại bỏ trùng lặp)
builder.Services.AddScoped<AuthService>();

// Đăng ký S3Service và IAmazonS3 cho S3 Express One Zone
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
        s3Config
    );
});

builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 29))));


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Movie API", Version = "v1" });

    // Cấu hình hỗ trợ upload file
    c.OperationFilter<SwaggerFileUploadOperationFilter>();
});

builder.Services.AddScoped<S3Service>();

// Thêm MemoryCache (đã đúng)
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

// Thêm CORS trước Authentication/Authorization
app.UseCors("AllowAll");

// Thêm middleware xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

// Thêm Swagger
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

// Map controllers
app.MapControllers();

app.Run();