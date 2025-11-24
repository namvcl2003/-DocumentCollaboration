// using System;
// using System.Collections.Generic;
//
// namespace DocumentCollaboration.Application.DTOs.Documents
// {
//     // ===== REQUEST DTOs =====
//
//     public class CreateDocumentRequest
//     {
//         public string Title { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int CategoryId { get; set; }
//         public int Priority { get; set; } = 2;
//         public DateTime? DueDate { get; set; }
//         // File will be uploaded separately via IFormFile
//     }
//
//     public class UpdateDocumentRequest
//     {
//         public string Title { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int CategoryId { get; set; }
//         public int Priority { get; set; }
//         public DateTime? DueDate { get; set; }
//     }
//
//     public class SubmitDocumentRequest
//     {
//         public int ToUserId { get; set; }
//         public string? Comments { get; set; }
//     }
//
//     public class ApproveDocumentRequest
//     {
//         public string? Comments { get; set; }
//         public bool SendToNextLevel { get; set; } = false;
//         public int? NextLevelUserId { get; set; }
//     }
//
//     public class RequestRevisionRequest
//     {
//         public int SendBackToUserId { get; set; }
//         public string Comments { get; set; } = string.Empty;
//     }
//
//     public class RejectDocumentRequest
//     {
//         public string Comments { get; set; } = string.Empty;
//     }
//
//     public class AddCommentRequest
//     {
//         public string CommentText { get; set; } = string.Empty;
//         public int? ParentCommentId { get; set; }
//     }
//
//     public class DocumentQueryRequest
//     {
//         public string? SearchTerm { get; set; }
//         public int? StatusId { get; set; }
//         public int? CategoryId { get; set; }
//         public int? Priority { get; set; }
//         public DateTime? FromDate { get; set; }
//         public DateTime? ToDate { get; set; }
//         public int PageNumber { get; set; } = 1;
//         public int PageSize { get; set; } = 20;
//         public string? SortBy { get; set; } = "CreatedAt";
//         public bool SortDescending { get; set; } = true;
//     }
//
//     // ===== RESPONSE DTOs =====
//
//     public class DocumentDto
//     {
//         public int DocumentId { get; set; }
//         public string DocumentNumber { get; set; } = string.Empty;
//         public string Title { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         
//         public int CategoryId { get; set; }
//         public string CategoryName { get; set; } = string.Empty;
//         
//         public int StatusId { get; set; }
//         public string StatusName { get; set; } = string.Empty;
//         public string StatusCode { get; set; } = string.Empty;
//         
//         public int CreatedByUserId { get; set; }
//         public string CreatedByName { get; set; } = string.Empty;
//         
//         public int? CurrentHandlerUserId { get; set; }
//         public string? CurrentHandlerName { get; set; }
//         public int CurrentWorkflowLevel { get; set; }
//         
//         public string FileName { get; set; } = string.Empty;
//         public string FileExtension { get; set; } = string.Empty;
//         public long? FileSize { get; set; }
//         
//         public int DepartmentId { get; set; }
//         public string DepartmentName { get; set; } = string.Empty;
//         
//         public int Priority { get; set; }
//         public string PriorityText { get; set; } = string.Empty;
//         
//         public DateTime? DueDate { get; set; }
//         public DateTime? SubmittedAt { get; set; }
//         public DateTime? ApprovedAt { get; set; }
//         public DateTime? RejectedAt { get; set; }
//         public DateTime? CompletedAt { get; set; }
//         public DateTime CreatedAt { get; set; }
//         public DateTime UpdatedAt { get; set; }
//         
//         public int CurrentVersion { get; set; }
//         public bool IsOverdue { get; set; }
//         public bool CanEdit { get; set; }
//         public bool CanSubmit { get; set; }
//         public bool CanApprove { get; set; }
//         public bool CanReject { get; set; }
//         public bool CanRequestRevision { get; set; }
//     }
//
//     public class DocumentDetailDto : DocumentDto
//     {
//         public List<DocumentVersionDto> Versions { get; set; } = new();
//         public List<WorkflowHistoryDto> WorkflowHistory { get; set; } = new();
//         public List<DocumentCommentDto> Comments { get; set; } = new();
//         public DocumentAssignmentDto? CurrentAssignment { get; set; }
//     }
//
//     public class DocumentVersionDto
//     {
//         public int VersionId { get; set; }
//         public int DocumentId { get; set; }
//         public int VersionNumber { get; set; }
//         public string FileName { get; set; } = string.Empty;
//         public long? FileSize { get; set; }
//         public int CreatedByUserId { get; set; }
//         public string CreatedByName { get; set; } = string.Empty;
//         public string? ChangeDescription { get; set; }
//         public bool IsCurrentVersion { get; set; }
//         public DateTime CreatedAt { get; set; }
//     }
//
//     public class WorkflowHistoryDto
//     {
//         public int HistoryId { get; set; }
//         public string ActionName { get; set; } = string.Empty;
//         public string ActionCode { get; set; } = string.Empty;
//         public int FromUserId { get; set; }
//         public string FromUserName { get; set; } = string.Empty;
//         public int? ToUserId { get; set; }
//         public string? ToUserName { get; set; }
//         public int FromWorkflowLevel { get; set; }
//         public int? ToWorkflowLevel { get; set; }
//         public string? Comments { get; set; }
//         public string? PreviousStatusName { get; set; }
//         public string NewStatusName { get; set; } = string.Empty;
//         public DateTime ActionAt { get; set; }
//     }
//
//     public class DocumentAssignmentDto
//     {
//         public int AssignmentId { get; set; }
//         public int DocumentId { get; set; }
//         public int AssignedToUserId { get; set; }
//         public string AssignedToName { get; set; } = string.Empty;
//         public int AssignedByUserId { get; set; }
//         public string AssignedByName { get; set; } = string.Empty;
//         public int WorkflowLevel { get; set; }
//         public bool IsActive { get; set; }
//         public DateTime? DueDate { get; set; }
//         public DateTime AssignedAt { get; set; }
//     }
//
//     public class DocumentCommentDto
//     {
//         public int CommentId { get; set; }
//         public int DocumentId { get; set; }
//         public int UserId { get; set; }
//         public string UserName { get; set; } = string.Empty;
//         public string CommentText { get; set; } = string.Empty;
//         public int? ParentCommentId { get; set; }
//         public bool IsResolved { get; set; }
//         public string? ResolvedByName { get; set; }
//         public DateTime? ResolvedAt { get; set; }
//         public DateTime CreatedAt { get; set; }
//         public List<DocumentCommentDto> Replies { get; set; } = new();
//     }
//
//     public class PagedResult<T>
//     {
//         public List<T> Items { get; set; } = new();
//         public int TotalCount { get; set; }
//         public int PageNumber { get; set; }
//         public int PageSize { get; set; }
//         public int TotalPages { get; set; }
//         public bool HasPreviousPage { get; set; }
//         public bool HasNextPage { get; set; }
//     }
//
//     public class DashboardStatsDto
//     {
//         public int TotalDocuments { get; set; }
//         public int PendingAssignments { get; set; }
//         public int DocumentsCreatedThisMonth { get; set; }
//         public int OverdueDocuments { get; set; }
//         public List<StatusStatDto> DocumentsByStatus { get; set; } = new();
//     }
//
//     public class StatusStatDto
//     {
//         public string StatusName { get; set; } = string.Empty;
//         public string StatusCode { get; set; } = string.Empty;
//         public int Count { get; set; }
//     }
//
//     public class DocumentCategoryDto
//     {
//         public int CategoryId { get; set; }
//         public string CategoryName { get; set; } = string.Empty;
//         public string CategoryCode { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public bool IsActive { get; set; }
//     }
//
//     public class DocumentStatusDto
//     {
//         public int StatusId { get; set; }
//         public string StatusName { get; set; } = string.Empty;
//         public string StatusCode { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public int DisplayOrder { get; set; }
//     }
// }



using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Application.DTOs.Documents
{
    // Base DTOs
    public class DocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string PriorityText { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int CurrentWorkflowLevel { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int? CurrentHandlerUserId { get; set; }
        public string? CurrentHandlerName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DocumentDetailDto : DocumentDto
    {
        public DocumentVersionDto? CurrentVersion { get; set; }
        public List<WorkflowHistoryDto> WorkflowHistory { get; set; } = new();
        public DocumentAssignmentDto? CurrentAssignment { get; set; }
        public bool CanEdit { get; set; }
        public bool CanSubmit { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanRequestRevision { get; set; }
    }

    public class DocumentVersionDto
    {
        public int VersionId { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? ChangeDescription { get; set; }
        public bool IsCurrentVersion { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WorkflowHistoryDto
    {
        public int HistoryId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public string FromUserName { get; set; } = string.Empty;
        public string? ToUserName { get; set; }
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public DateTime ActionAt { get; set; }
    }

    public class DocumentAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int DocumentId { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToName { get; set; } = string.Empty;
        public int AssignedByUserId { get; set; }
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class DocumentCommentDto
    {
        public int CommentId { get; set; }
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public bool IsResolved { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DocumentCommentDto> Replies { get; set; } = new();
    }

    public class DocumentCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class DocumentStatusDto
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalDocuments { get; set; }
        public int PendingAssignments { get; set; }
        public int DocumentsCreatedThisMonth { get; set; }
        public int OverdueDocuments { get; set; }
        public List<StatusStatDto> DocumentsByStatus { get; set; } = new();
    }

    public class StatusStatDto
    {
        public string StatusName { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // Request DTOs
    public class CreateDocumentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateDocumentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class SubmitDocumentRequest
    {
        public int ToUserId { get; set; }
        public string? Comments { get; set; }
    }

    public class ApproveDocumentRequest
    {
        public bool SendToNextLevel { get; set; }
        public int? NextLevelUserId { get; set; }
        public string? Comments { get; set; }
    }

    public class RejectDocumentRequest
    {
        public string Comments { get; set; } = string.Empty;
    }

    public class RequestRevisionRequest
    {
        public int SendBackToUserId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class AddCommentRequest
    {
        public string CommentText { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
    }

    public class DocumentQueryRequest
    {
        public string? SearchTerm { get; set; }
        public int? StatusId { get; set; }
        public int? CategoryId { get; set; }
        public int? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    // Paged Result
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}