using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentCollaboration.Infrastructure.Services;

namespace DocumentCollaboration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get user notifications with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
            [FromQuery] bool? isRead = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var notifications = await _notificationService.GetUserNotificationsAsync(
                    userId, isRead, pageNumber, pageSize);

                var notificationDtos = notifications.Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    DocumentId = n.DocumentId,
                    NotificationType = n.NotificationType.TypeName
                }).ToList();

                return Ok(notificationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải thông báo" });
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải số thông báo" });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            try
            {
                var success = await _notificationService.MarkAsReadAsync(id);
                
                if (!success)
                    return NotFound(new { message = "Không tìm thấy thông báo" });

                return Ok(new { message = "Đã đánh dấu đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read {NotificationId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi" });
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPut("mark-all-read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var success = await _notificationService.MarkAllAsReadAsync(userId);
                
                if (!success)
                    return BadRequest(new { message = "Không thể đánh dấu thông báo" });

                return Ok(new { message = "Đã đánh dấu tất cả đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "Đã xảy ra lỗi" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) 
                ? userId 
                : 0;
        }
    }

    // DTO
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? DocumentId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
    }
}