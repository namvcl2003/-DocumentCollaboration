using Microsoft.AspNetCore.Http;
using DocumentCollaboration.Application.DTOs.Documents;

namespace DocumentCollaboration.Application.Services
{
    public interface IDocumentService
    {
        // Document CRUD
        Task<PagedResult<DocumentDto>> GetDocumentsByUserAsync(int userId, DocumentQueryRequest request);
        Task<DocumentDetailDto?> GetDocumentDetailAsync(int documentId, int userId);
        Task<DocumentDto?> CreateDocumentAsync(int userId, CreateDocumentRequest request, IFormFile file);
        Task<DocumentDto?> UpdateDocumentAsync(int documentId, int userId, UpdateDocumentRequest request);
        
        // Workflow operations
        Task<bool> SubmitDocumentAsync(int documentId, int userId, SubmitDocumentRequest request);
        Task<bool> ApproveDocumentAsync(int documentId, int userId, ApproveDocumentRequest request);
        Task<bool> RejectDocumentAsync(int documentId, int userId, RejectDocumentRequest request);
        Task<bool> RequestRevisionAsync(int documentId, int userId, RequestRevisionRequest request);
        
        // Version management
        Task<DocumentVersionDto?> CreateDocumentVersionAsync(int documentId, int userId, IFormFile file, string? changeDescription);
        Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(int documentId);
        
        // Comments
        Task<DocumentCommentDto?> AddCommentAsync(int documentId, int userId, AddCommentRequest request);
        Task<List<DocumentCommentDto>> GetDocumentCommentsAsync(int documentId);
        
        // File operations
        Task<(bool Success, byte[]? FileBytes, string? FileName, string? ContentType)> GetDocumentFileAsync(
            int documentId, int userId, int? versionId = null);
        
        // Lookup data
        Task<List<DocumentCategoryDto>> GetCategoriesAsync();
        Task<List<DocumentStatusDto>> GetStatusesAsync();
        Task<DashboardStatsDto> GetDashboardStatsAsync(int userId);
    }
}