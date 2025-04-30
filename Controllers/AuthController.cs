// Controllers/AuthController.cs
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
            var refreshToken = _authService.GenerateRefreshToken();
            await _authService.SaveRefreshToken(user.Id, refreshToken);

            return Ok(new { AccessToken = accessToken, RefreshToken = refreshToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.RevokeRefreshToken(request.RefreshToken);
            return Ok("Logged out successfully");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Gửi OTP trước khi đăng ký
            var otpSent = await _authService.SendOtp(request.Email);
            if (!otpSent)
                return BadRequest("Failed to send OTP");

            // Lưu thông tin tạm (email, password) nếu cần, chờ xác thực OTP
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

            // Sau khi OTP hợp lệ, hoàn tất đăng ký
            var success = await _authService.RegisterUser(request.Email, request.Password);
            if (!success)
                return BadRequest("Email already exists");

            return Ok("User registered successfully");
        }

    [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var user = await _authService.ValidateRefreshToken(request.RefreshToken);
            if (user == null)
                return Unauthorized("Invalid or expired refresh token");

            var newAccessToken = _authService.GenerateJwtToken(user);
            var newRefreshToken = _authService.GenerateRefreshToken();
            await _authService.RevokeRefreshToken(request.RefreshToken); // Thu hồi refresh token cũ
            await _authService.SaveRefreshToken(user.Id, newRefreshToken); // Lưu refresh token mới

            return Ok(new { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
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