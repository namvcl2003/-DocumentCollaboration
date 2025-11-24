using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentCollaboration.Application.DTOs.Auth;
using DocumentCollaboration.Application.Services;

namespace DocumentCollaboration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách người dùng" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải thông tin người dùng" });
            }
        }

        /// <summary>
        /// Get users by department
        /// </summary>
        [HttpGet("department/{departmentId}")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDto>>> GetUsersByDepartment(int departmentId)
        {
            try
            {
                var users = await _userService.GetUsersByDepartmentAsync(departmentId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for department {DepartmentId}", departmentId);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách người dùng" });
            }
        }

        /// <summary>
        /// Get users by role level
        /// </summary>
        [HttpGet("role/{roleLevel}")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDto>>> GetUsersByRole(int roleLevel)
        {
            try
            {
                var users = await _userService.GetUsersByRoleLevelAsync(roleLevel);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for role level {RoleLevel}", roleLevel);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách người dùng" });
            }
        }

        /// <summary>
        /// Update user information (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userService.UpdateUserAsync(id, request);
                
                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật người dùng" });
            }
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        [HttpPut("{id}/deactivate")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            try
            {
                var success = await _userService.DeactivateUserAsync(id);
                
                if (!success)
                    return NotFound(new { message = "Không tìm thấy người dùng" });

                return Ok(new { message = "Vô hiệu hóa người dùng thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi vô hiệu hóa người dùng" });
            }
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            try
            {
                var roles = await _userService.GetRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách vai trò" });
            }
        }

        /// <summary>
        /// Get all departments
        /// </summary>
        [HttpGet("departments")]
        [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DepartmentDto>>> GetDepartments()
        {
            try
            {
                var departments = await _userService.GetDepartmentsAsync();
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách phòng ban" });
            }
        }
    }

    // DTOs for User operations
    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ManagerName { get; set; }
        public string? ViceManagerName { get; set; }
        public bool IsActive { get; set; }
    }
}