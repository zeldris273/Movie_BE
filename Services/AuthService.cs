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

            var cmd = new MySqlCommand("SELECT Id, Email, PasswordHash FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? "" : reader.GetString(reader.GetOrdinal("PasswordHash"))
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
                new Claim(ClaimTypes.Name, user.Email)
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
