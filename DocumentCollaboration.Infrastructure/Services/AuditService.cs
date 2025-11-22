using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocumentCollaboration.Domain.Entities;
using DocumentCollaboration.Domain.Interfaces;

namespace DocumentCollaboration.Infrastructure.Services
{
    /// <summary>
    /// Interface for audit logging service
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(
            int? userId,
            string actionType,
            string? entityType = null,
            int? entityId = null,
            string? actionDescription = null,
            object? oldValue = null,
            object? newValue = null,
            string? ipAddress = null,
            string? userAgent = null);

        Task LogDocumentActionAsync(
            int userId,
            string actionType,
            int documentId,
            string actionDescription,
            object? oldValue = null,
            object? newValue = null);

        Task LogUserActionAsync(
            int userId,
            string actionType,
            string actionDescription,
            string? ipAddress = null,
            string? userAgent = null);
    }

    /// <summary>
    /// Service for audit logging
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task LogAsync(
            int? userId,
            string actionType,
            string? entityType = null,
            int? entityId = null,
            string? actionDescription = null,
            object? oldValue = null,
            object? newValue = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    ActionDescription = actionDescription,
                    OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                    NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Audit log created: {ActionType} by user {UserId}", actionType, userId);
            }
            catch (Exception ex)
            {
                // Don't throw - audit logging should not break the main operation
                _logger.LogError(ex, "Error creating audit log for action: {ActionType}", actionType);
            }
        }

        public async Task LogDocumentActionAsync(
            int userId,
            string actionType,
            int documentId,
            string actionDescription,
            object? oldValue = null,
            object? newValue = null)
        {
            await LogAsync(
                userId: userId,
                actionType: actionType,
                entityType: "Document",
                entityId: documentId,
                actionDescription: actionDescription,
                oldValue: oldValue,
                newValue: newValue
            );
        }

        public async Task LogUserActionAsync(
            int userId,
            string actionType,
            string actionDescription,
            string? ipAddress = null,
            string? userAgent = null)
        {
            await LogAsync(
                userId: userId,
                actionType: actionType,
                entityType: "User",
                entityId: userId,
                actionDescription: actionDescription,
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }
    }
}