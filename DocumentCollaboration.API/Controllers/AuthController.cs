using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentCollaboration.Application.DTOs.Auth;
using DocumentCollaboration.Application.Services;

namespace DocumentCollaboration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.LoginAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                    return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });
                }

                _logger.LogInformation("User {Username} logged in successfully", request.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng nhập" });
            }
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [Authorize(Policy = "RequireAdmin")] // Only admin can register new users
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.RegisterAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("Registration failed for username: {Username} - User already exists", request.Username);
                    return Conflict(new { message = "Tên đăng nhập hoặc email đã tồn tại" });
                }

                _logger.LogInformation("New user registered: {Username}", request.Username);
                return CreatedAtAction(nameof(GetCurrentUser), new { id = result.UserId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng ký" });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return BadRequest(new { message = "Refresh token không hợp lệ" });

                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (result == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình làm mới token" });
            }
        }

        /// <summary>
        /// Logout - Revoke refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return BadRequest(new { message = "Refresh token không hợp lệ" });

                var result = await _authService.RevokeTokenAsync(request.RefreshToken);

                if (!result)
                    return BadRequest(new { message = "Không thể thu hồi token" });

                _logger.LogInformation("User logged out successfully");
                return Ok(new { message = "Đăng xuất thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đăng xuất" });
            }
        }

        /// <summary>
        /// Change password for current user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                var result = await _authService.ChangePasswordAsync(userId, request);

                if (!result)
                {
                    _logger.LogWarning("Failed password change attempt for user ID: {UserId}", userId);
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
                }

                _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                return StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình đổi mật khẩu" });
            }
        }

        /// <summary>
        /// Get current authenticated user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized();

                // You would implement GetUserById in AuthService or create UserService
                // For now, return basic info from claims
                return Ok(new UserDto
                {
                    UserId = userId,
                    Username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
                    Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
                    FullName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "",
                    RoleName = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "",
                    RoleLevel = int.Parse(User.FindFirst("RoleLevel")?.Value ?? "0"),
                    DepartmentId = int.TryParse(User.FindFirst("DepartmentId")?.Value, out int deptId) ? deptId : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin người dùng" });
            }
        }

        /// <summary>
        /// Validate token (check if token is still valid)
        /// </summary>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult ValidateToken()
        {
            return Ok(new
            {
                valid = true,
                message = "Token is valid",
                userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            });
        }
    }
}