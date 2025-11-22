using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Domain.Entities
{
    /// <summary>
    /// User entity - represents system users
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<Document> HandlingDocuments { get; set; } = new List<Document>();
        public virtual ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
        public virtual ICollection<DocumentAssignment> AssignedDocuments { get; set; } = new List<DocumentAssignment>();
        public virtual ICollection<DocumentAssignment> CreatedAssignments { get; set; } = new List<DocumentAssignment>();
        public virtual ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}