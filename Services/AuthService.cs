using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MySql.Data.MySqlClient;
using BCrypt.Net;
using System.Net.Mail;
using System.Net;
using backend.Models;

namespace backend.Services
{
    public class AuthService
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;

        public AuthService(IConfiguration config, IMemoryCache cache)
        {
            _config = config;
            _cache = cache;
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }

        public async Task<bool> RegisterUser(string email, string password)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
            checkCmd.Parameters.AddWithValue("@Email", email.Trim());
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count > 0)
                return false;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var insertCmd = new MySqlCommand(
                "INSERT INTO Users (Email, PasswordHash) VALUES (@Email, @PasswordHash)",
                conn);
            insertCmd.Parameters.AddWithValue("@Email", email.Trim());
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var rowsAffected = await insertCmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<User> ValidateUser(string email, string password)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT Id, Email, PasswordHash, Role FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? "" : reader.GetString(reader.GetOrdinal("PasswordHash")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "User" : reader.GetString(reader.GetOrdinal("Role")),
                };

                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    return user;
            }
            return null;
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1), // Access token hết hạn sau 1 giờ
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Tạo refresh token dưới dạng JWT
        public string GenerateRefreshToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:RefreshKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7), // Refresh token hết hạn sau 7 ngày (giảm thời gian sống để tăng bảo mật)
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Xác thực refresh token (dạng JWT)
        public User ValidateRefreshToken(string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:RefreshKey"]);

            try
            {
                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return null;

                return new User
                {
                    Id = int.Parse(userIdClaim),
                    Email = principal.FindFirst(ClaimTypes.Name)?.Value,
                    Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User"
                };
            }
            catch
            {
                return null; // Token không hợp lệ hoặc đã hết hạn
            }
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<bool> SendOtp(string email)
        {
            email = email.Trim();
            if (string.IsNullOrEmpty(email)) return false;

            var otp = GenerateOtp();
            try
            {
                var smtpClient = new SmtpClient(_config["Smtp:Host"])
                {
                    Port = int.Parse(_config["Smtp:Port"]),
                    Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config["Smtp:Username"]),
                    Subject = "Your OTP Code",
                    Body = $"Your OTP code is: {otp}. It is valid for 5 minutes.",
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                _cache.Set(email, (otp, DateTime.UtcNow.AddMinutes(5)), TimeSpan.FromMinutes(5));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyOtp(string email, string otp)
        {
            email = email.Trim();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp)) return false;

            if (_cache.TryGetValue(email, out (string storedOtp, DateTime expires) stored) && DateTime.UtcNow <= stored.expires && stored.storedOtp == otp)
            {
                _cache.Remove(email);
                return true;
            }
            return false;
        }

        public async Task<bool> EmailExists(string email)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email.Trim());
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // Thêm phương thức để đặt lại mật khẩu
        public async Task<bool> ResetPassword(string email, string newPassword)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var cmd = new MySqlCommand(
                "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email",
                conn);
            cmd.Parameters.AddWithValue("@Email", email.Trim());
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}