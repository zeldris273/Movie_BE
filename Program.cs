using Amazon.S3;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ controllers
builder.Services.AddControllers();

// Thêm Swagger
builder.Services.AddSwaggerGen();

// Đăng ký AuthService
builder.Services.AddScoped<AuthService>();

// Đăng ký S3Service và IAmazonS3
builder.Services.AddSingleton<IAmazonS3>(sp =>
    new AmazonS3Client(
        builder.Configuration["AWS:AccessKey"],
        builder.Configuration["AWS:SecretKey"],
        Amazon.RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"])
    ));
builder.Services.AddScoped<S3Service>();

// Thêm cấu hình xác thực JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,              // Xác minh Issuer
            ValidateAudience = true,            // Xác minh Audience
            ValidateLifetime = true,            // Xác minh thời hạn token
            ValidateIssuerSigningKey = true,    // Xác minh khóa ký
            ValidIssuer = builder.Configuration["Jwt:Issuer"],    // Giá trị từ appsettings.json
            ValidAudience = builder.Configuration["Jwt:Audience"], // Giá trị từ appsettings.json
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // Khóa bí mật
        };
    });

// Thêm CORS (nếu frontend chạy trên domain/cổng khác, như localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()   // Cho phép tất cả origin (có thể giới hạn cụ thể)
               .AllowAnyMethod()   // Cho phép tất cả HTTP methods (GET, POST, ...)
               .AllowAnyHeader();  // Cho phép tất cả header (Authorization, ...)
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