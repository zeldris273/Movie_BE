using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.ValidateUser(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Invalid credentials");

            var accessToken = _authService.GenerateJwtToken(user);
            var refreshToken = _authService.GenerateRefreshToken(user);

            // Thêm cookie cho refresh token
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Bảo vệ cookie khỏi XSS
                Secure = true,   // Chỉ gửi qua HTTPS (dùng false khi test localhost)
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None // Cho phép cross-site (thay đổi tùy môi trường)
            };
            Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

            return Ok(new { AccessToken = accessToken });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Vì refreshToken là stateless, không thể thu hồi token.
            // Client nên xóa token ở phía frontend.
            return Ok("Logged out successfully. Please clear your tokens on the client side.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var otpSent = await _authService.SendOtp(request.Email);
            if (!otpSent)
                return BadRequest("Failed to send OTP");

            return Ok("OTP sent to your email. Please verify to complete registration.");
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var success = await _authService.SendOtp(request.Email);
            if (!success)
                return BadRequest("Failed to send OTP");

            return Ok("OTP sent successfully");
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var isValid = _authService.VerifyOtp(request.Email, request.Otp);
            if (!isValid)
                return BadRequest("Invalid OTP");

            var success = await _authService.RegisterUser(request.Email, request.Password);
            if (!success)
                return BadRequest("Email already exists");

            return Ok("User registered successfully");
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            // Lấy refresh token từ cookie
            if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                return Unauthorized("Refresh token not found");

            var user = _authService.ValidateRefreshToken(refreshToken);
            if (user == null)
                return Unauthorized("Invalid or expired refresh token");

            var newAccessToken = _authService.GenerateJwtToken(user);
            var newRefreshToken = _authService.GenerateRefreshToken(user);

            // Cập nhật cookie với refresh token mới
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("RefreshToken", newRefreshToken, cookieOptions);

            return Ok(new { AccessToken = newAccessToken });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SendOtpRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Otp { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}