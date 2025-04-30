using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
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
                new Claim(ClaimTypes.Name, user.Email),
                 new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Tạo refresh token
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // Lưu refresh token vào database
        public async Task SaveRefreshToken(int userId, string refreshToken)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "INSERT INTO RefreshToken (UserId, Token, IssuedAt, ExpiresAt, IsRevoked) " +
                "VALUES (@UserId, @Token, @IssuedAt, @ExpiresAt, @IsRevoked)",
                conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Token", refreshToken);
            cmd.Parameters.AddWithValue("@IssuedAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddDays(30)); // Hết hạn sau 30 ngày
            cmd.Parameters.AddWithValue("@IsRevoked", false);

            await cmd.ExecuteNonQueryAsync();
        }

        // Xác thực refresh token
        public async Task<User> ValidateRefreshToken(string refreshToken)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cleanupCmd = new MySqlCommand(
        "DELETE FROM RefreshToken WHERE ExpiresAt < @Now OR IsRevoked = true",
        conn);
            cleanupCmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
            await cleanupCmd.ExecuteNonQueryAsync();

            var cmd = new MySqlCommand(
                "SELECT rt.*, u.Id, u.Email, u.Role " +
                "FROM RefreshToken rt " +
                "JOIN Users u ON rt.UserId = u.Id " +
                "WHERE rt.Token = @Token AND rt.IsRevoked = false AND rt.ExpiresAt > @Now",
                conn);
            cmd.Parameters.AddWithValue("@Token", refreshToken);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "User" : reader.GetString(reader.GetOrdinal("Role")),
                };
            }
            return null;
        }

        // Thu hồi refresh token
        public async Task RevokeRefreshToken(string refreshToken)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand(
                "UPDATE RefreshToken SET IsRevoked = true WHERE Token = @Token",
                conn);
            cmd.Parameters.AddWithValue("@Token", refreshToken);

            await cmd.ExecuteNonQueryAsync();
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

    }
}
