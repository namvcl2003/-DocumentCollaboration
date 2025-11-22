using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Domain.Entities
{
    // ===== DEPARTMENT =====
    public class Department
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public int? ManagerUserId { get; set; }
        public int? ViceManagerUserId { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual User? Manager { get; set; }
        public virtual User? ViceManager { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    // ===== DOCUMENT CATEGORY =====
    public class DocumentCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    // ===== DOCUMENT STATUS =====
    public class DocumentStatus
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    // ===== DOCUMENT VERSION =====
    public class DocumentVersion
    {
        public int VersionId { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public int CreatedByUserId { get; set; }
        public string? ChangeDescription { get; set; }
        public bool IsCurrentVersion { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        public virtual Document Document { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual ICollection<DocumentWorkflowHistory> WorkflowHistory { get; set; } = new List<DocumentWorkflowHistory>();
        public virtual ICollection<CollaboraSession> CollaboraSessions { get; set; } = new List<CollaboraSession>();
    }

    // ===== WORKFLOW ACTION =====
    public class WorkflowAction
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public string? Description { get; set; }

        public virtual ICollection<DocumentWorkflowHistory> WorkflowHistory { get; set; } = new List<DocumentWorkflowHistory>();
    }

    // ===== DOCUMENT WORKFLOW HISTORY =====
    public class DocumentWorkflowHistory
    {
        public int HistoryId { get; set; }
        public int DocumentId { get; set; }
        public int? VersionId { get; set; }
        public int ActionId { get; set; }
        public int FromUserId { get; set; }
        public int? ToUserId { get; set; }
        public int FromWorkflowLevel { get; set; }
        public int? ToWorkflowLevel { get; set; }
        public string? Comments { get; set; }
        public int? PreviousStatusId { get; set; }
        public int NewStatusId { get; set; }
        public DateTime ActionAt { get; set; }

        public virtual Document Document { get; set; } = null!;
        public virtual DocumentVersion? Version { get; set; }
        public virtual WorkflowAction Action { get; set; } = null!;
        public virtual User FromUser { get; set; } = null!;
        public virtual User? ToUser { get; set; }
        public virtual DocumentStatus? PreviousStatus { get; set; }
        public virtual DocumentStatus NewStatus { get; set; } = null!;
    }

    // ===== DOCUMENT ASSIGNMENT =====
    public class DocumentAssignment
    {
        public int AssignmentId { get; set; }
        public int DocumentId { get; set; }
        public int AssignedToUserId { get; set; }
        public int AssignedByUserId { get; set; }
        public int WorkflowLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DueDate { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public virtual Document Document { get; set; } = null!;
        public virtual User AssignedToUser { get; set; } = null!;
        public virtual User AssignedByUser { get; set; } = null!;
    }

    // ===== COLLABORA SESSION =====
    public class CollaboraSession
    {
        public int SessionId { get; set; }
        public int DocumentId { get; set; }
        public int? VersionId { get; set; }
        public int UserId { get; set; }
        public string? CollaboraAccessToken { get; set; }
        public string? CollaboraSessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? AccessMode { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public virtual Document Document { get; set; } = null!;
        public virtual DocumentVersion? Version { get; set; }
        public virtual User User { get; set; } = null!;
    }

    // ===== DOCUMENT COMMENT =====
    public class DocumentComment
    {
        public int CommentId { get; set; }
        public int DocumentId { get; set; }
        public int? VersionId { get; set; }
        public int UserId { get; set; }
        public string CommentText { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public bool IsResolved { get; set; } = false;
        public int? ResolvedByUserId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Document Document { get; set; } = null!;
        public virtual DocumentVersion? Version { get; set; }
        public virtual User User { get; set; } = null!;
        public virtual DocumentComment? ParentComment { get; set; }
        public virtual User? ResolvedByUser { get; set; }
        public virtual ICollection<DocumentComment> Replies { get; set; } = new List<DocumentComment>();
    }

    // ===== NOTIFICATION TYPE =====
    public class NotificationType
    {
        public int NotificationTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string? Description { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

    // ===== NOTIFICATION =====
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int NotificationTypeId { get; set; }
        public int? DocumentId { get; set; }
        public int? CommentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual NotificationType NotificationType { get; set; } = null!;
        public virtual Document? Document { get; set; }
        public virtual DocumentComment? Comment { get; set; }
    }

    // ===== AUDIT LOG =====
    public class AuditLog
    {
        public long AuditId { get; set; }
        public int? UserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? ActionDescription { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }

    // ===== REFRESH TOKEN =====
    public class RefreshToken
    {
        public int TokenId { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsRevoked { get; set; } = false;

        public virtual User User { get; set; } = null!;
    }

    // ===== SYSTEM SETTING =====
    public class SystemSetting
    {
        public int SettingId { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string? SettingValue { get; set; }
        public string? SettingType { get; set; }
        public string? Description { get; set; }
        public bool IsEditable { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}