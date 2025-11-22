using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DocumentCollaboration.Domain.Entities;
using DocumentCollaboration.Domain.Interfaces;

namespace DocumentCollaboration.Infrastructure.Services
{
    /// <summary>
    /// Interface for notification service
    /// </summary>
    public interface INotificationService
    {
        Task<bool> CreateNotificationAsync(
            int userId, 
            string notificationTypeCode, 
            string title, 
            string message,
            int? documentId = null,
            int? commentId = null,
            string? link = null);

        Task<bool> CreateBulkNotificationsAsync(
            IEnumerable<int> userIds,
            string notificationTypeCode,
            string title,
            string message,
            int? documentId = null,
            int? commentId = null,
            string? link = null);

        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<Notification>> GetUserNotificationsAsync(
            int userId, 
            bool? isRead = null, 
            int pageNumber = 1, 
            int pageSize = 20);
    }

    /// <summary>
    /// Service for managing notifications
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> CreateNotificationAsync(
            int userId,
            string notificationTypeCode,
            string title,
            string message,
            int? documentId = null,
            int? commentId = null,
            string? link = null)
        {
            try
            {
                // Get notification type
                var notificationType = await _unitOfWork.NotificationTypes
                    .SingleOrDefaultAsync(nt => nt.TypeCode == notificationTypeCode);

                if (notificationType == null)
                {
                    _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                    return false;
                }

                var notification = new Notification
                {
                    UserId = userId,
                    NotificationTypeId = notificationType.NotificationTypeId,
                    DocumentId = documentId,
                    CommentId = commentId,
                    Title = title,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CreateBulkNotificationsAsync(
            IEnumerable<int> userIds,
            string notificationTypeCode,
            string title,
            string message,
            int? documentId = null,
            int? commentId = null,
            string? link = null)
        {
            try
            {
                var notificationType = await _unitOfWork.NotificationTypes
                    .SingleOrDefaultAsync(nt => nt.TypeCode == notificationTypeCode);

                if (notificationType == null)
                {
                    _logger.LogWarning("Notification type not found: {TypeCode}", notificationTypeCode);
                    return false;
                }

                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    NotificationTypeId = notificationType.NotificationTypeId,
                    DocumentId = documentId,
                    CommentId = commentId,
                    Title = title,
                    Message = message,
                    Link = link,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.Notifications.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk notifications created for {Count} users", userIds.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk notifications");
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
                
                if (notification == null)
                {
                    return false;
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _unitOfWork.Notifications.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _unitOfWork.Notifications
                    .FindAsync(n => n.UserId == userId && !n.IsRead);

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _unitOfWork.Notifications.UpdateRangeAsync(unreadNotifications);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _unitOfWork.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(
            int userId,
            bool? isRead = null,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _unitOfWork.Notifications
                    .QueryWithIncludes(n => n.NotificationType, n => n.Document!)
                    .Where(n => n.UserId == userId);

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return new List<Notification>();
            }
        }
    }
}