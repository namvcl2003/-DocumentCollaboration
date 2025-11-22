using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Domain.Entities
{
    /// <summary>
    /// Document entity - main entity for document management
    /// </summary>
    public class Document
    {
        public int DocumentId { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int StatusId { get; set; }
        
        // Creator and handler
        public int CreatedByUserId { get; set; }
        public int? CurrentHandlerUserId { get; set; }
        
        // Workflow tracking
        public int CurrentWorkflowLevel { get; set; } = 1; // 1, 2, or 3
        
        // File information
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? CollaboraFileId { get; set; }
        
        // Metadata
        public int DepartmentId { get; set; }
        public int Priority { get; set; } = 2; // 1=High, 2=Medium, 3=Low
        public DateTime? DueDate { get; set; }
        
        // Timestamps
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual DocumentCategory Category { get; set; } = null!;
        public virtual DocumentStatus Status { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? CurrentHandlerUser { get; set; }
        public virtual Department Department { get; set; } = null!;
        
        public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
        public virtual ICollection<DocumentWorkflowHistory> WorkflowHistory { get; set; } = new List<DocumentWorkflowHistory>();
        public virtual ICollection<DocumentAssignment> Assignments { get; set; } = new List<DocumentAssignment>();
        public virtual ICollection<DocumentComment> Comments { get; set; } = new List<DocumentComment>();
        public virtual ICollection<CollaboraSession> CollaboraSessions { get; set; } = new List<CollaboraSession>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}