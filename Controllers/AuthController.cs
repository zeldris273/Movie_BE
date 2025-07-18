using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly SignInManager<CustomUser> _signInManager;

        public AuthController(AuthService authService, SignInManager<CustomUser> signInManager)
        {
            _signInManager = signInManager;
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.ValidateUser(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Invalid credentials");

            var accessToken = await _authService.GenerateJwtToken(user); // Sử dụng await
            var refreshToken = await _authService.GenerateRefreshToken(user); // Sử dụng await

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

            return Ok(new { AccessToken = accessToken });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
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
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                return Unauthorized("Refresh token not found");

            var user = await _authService.ValidateRefreshToken(refreshToken);
            if (user == null)
                return Unauthorized("Invalid or expired refresh token");

            var newAccessToken = await _authService.GenerateJwtToken(user); // Sử dụng await
            var newRefreshToken = await _authService.GenerateRefreshToken(user); // Sử dụng await

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None
            };
            // Xóa cookie cũ trước khi thêm mới
            Response.Cookies.Delete("RefreshToken");
            Response.Cookies.Append("RefreshToken", newRefreshToken, cookieOptions);

            return Ok(new { AccessToken = newAccessToken });
        }

        // API để yêu cầu đặt lại mật khẩu
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var emailExists = await _authService.EmailExists(request.Email);
            if (!emailExists)
                return BadRequest("Email does not exist");

            var success = await _authService.SendOtp(request.Email);
            if (!success)
                return BadRequest("Failed to send OTP");

            return Ok("OTP sent to your email. Please verify to reset your password.");
        }

        // API để xác minh OTP và đặt lại mật khẩu
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var isValid = _authService.VerifyOtp(request.Email, request.Otp);
            if (!isValid)
                return BadRequest("Invalid OTP");

            var success = await _authService.ResetPassword(request.Email, request.Password);
            if (!success)
                return BadRequest("Failed to reset password");

            return Ok("Password reset successfully");
        }

        // Khởi động đăng nhập Google
        [HttpGet("login/google")]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { provider = "Google" }, Request.Scheme);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        // Khởi động đăng nhập GitHub
        [HttpGet("login/github")]
        public IActionResult LoginWithGitHub()
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { provider = "GitHub" }, Request.Scheme);
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("GitHub", redirectUrl);
            return Challenge(properties, "GitHub");
        }

        // Xử lý callback từ Google/GitHub
        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string provider, string returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return Unauthorized("External login information not found.");

            var user = await _authService.HandleExternalLogin(provider, info);
            if (user == null)
                return Unauthorized("External login failed.");

            var accessToken = await _authService.GenerateJwtToken(user);
            var refreshToken = await _authService.GenerateRefreshToken(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

            // Chuyển hướng về frontend với token (có thể điều chỉnh URL)
            string frontendUrl = "http://localhost:5173/auth";
            return Redirect($"{frontendUrl}?token={Uri.EscapeDataString(accessToken)}");
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

    // Thêm DTO cho quên mật khẩu
    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string Password { get; set; }
    }
}