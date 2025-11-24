using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DocumentCollaboration.Application.DTOs.Documents;
using DocumentCollaboration.Domain.Entities;
using DocumentCollaboration.Domain.Enums;
using DocumentCollaboration.Domain.Interfaces;
using DocumentCollaboration.Infrastructure.Services;

namespace DocumentCollaboration.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IDocumentNumberGenerator _documentNumberGenerator;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            IDocumentNumberGenerator documentNumberGenerator,
            INotificationService notificationService,
            IAuditService auditService,
            ILogger<DocumentService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _documentNumberGenerator = documentNumberGenerator;
            _notificationService = notificationService;
            _auditService = auditService;
            _logger = logger;
        }

        #region CRUD Operations

        public async Task<PagedResult<DocumentDto>> GetDocumentsByUserAsync(int userId, DocumentQueryRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.QueryWithIncludes(u => u.Role)
                    .SingleOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return new PagedResult<DocumentDto>();

                var query = _unitOfWork.Documents.QueryWithIncludes(
                    d => d.Category,
                    d => d.Status,
                    d => d.CreatedByUser,
                    d => d.CurrentHandlerUser!,
                    d => d.Department
                ).AsQueryable();

                // Filter by role level
                if (user.Role.RoleLevel == 1) // Trợ lý
                {
                    query = query.Where(d =>
                        d.CreatedByUserId == userId ||
                        d.CurrentHandlerUserId == userId);
                }
                else if (user.Role.RoleLevel == 2) // Phó phòng
                {
                    query = query.Where(d =>
                        d.DepartmentId == user.DepartmentId &&
                        (d.CreatedByUserId == userId ||
                         d.CurrentHandlerUserId == userId ||
                         d.CurrentWorkflowLevel >= 2));
                }
                else // Trưởng phòng, Admin
                {
                    if (user.DepartmentId.HasValue)
                    {
                        query = query.Where(d => d.DepartmentId == user.DepartmentId);
                    }
                }

                // Apply filters
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    query = query.Where(d =>
                        d.DocumentNumber.ToLower().Contains(searchLower) ||
                        d.Title.ToLower().Contains(searchLower) ||
                        d.Description!.ToLower().Contains(searchLower));
                }

                if (request.StatusId.HasValue)
                    query = query.Where(d => d.StatusId == request.StatusId.Value);

                if (request.CategoryId.HasValue)
                    query = query.Where(d => d.CategoryId == request.CategoryId.Value);

                if (request.Priority.HasValue)
                    query = query.Where(d => d.Priority == request.Priority.Value);

                if (request.FromDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= request.FromDate.Value);

                if (request.ToDate.HasValue)
                    query = query.Where(d => d.CreatedAt <= request.ToDate.Value);

                // Get total count
                var totalItems = await query.CountAsync();

                // Sort
                query = request.SortBy?.ToLower() switch
                {
                    "title" => request.SortDescending
                        ? query.OrderByDescending(d => d.Title)
                        : query.OrderBy(d => d.Title),
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(d => d.CreatedAt)
                        : query.OrderBy(d => d.CreatedAt),
                    "priority" => request.SortDescending
                        ? query.OrderByDescending(d => d.Priority)
                        : query.OrderBy(d => d.Priority),
                    _ => query.OrderByDescending(d => d.CreatedAt)
                };

                // Paginate
                var documents = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var documentDtos = documents.Select(d => MapToDocumentDto(d)).ToList();

                return new PagedResult<DocumentDto>
                {
                    Items = documentDtos,
                    TotalItems = totalItems,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for user {UserId}", userId);
                throw;
            }
        }

        public async Task<DocumentDetailDto?> GetDocumentDetailAsync(int documentId, int userId)
        {
            try
            {
                var document = await _unitOfWork.Documents.QueryWithIncludes(
                    d => d.Category,
                    d => d.Status,
                    d => d.CreatedByUser,
                    d => d.CurrentHandlerUser!,
                    d => d.Department
                ).SingleOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null) return null;

                if (!await CanUserAccessDocument(document, userId))
                    return null;

                var currentVersion = await _unitOfWork.DocumentVersions
                    .QueryWithIncludes(v => v.CreatedByUser)
                    .Where(v => v.DocumentId == documentId && v.IsCurrentVersion)
                    .FirstOrDefaultAsync();

                var workflowHistory = await GetWorkflowHistoryAsync(documentId);
                var currentAssignment = await GetCurrentAssignmentAsync(documentId);

                return new DocumentDetailDto
                {
                    DocumentId = document.DocumentId,
                    DocumentNumber = document.DocumentNumber,
                    Title = document.Title,
                    Description = document.Description,
                    CategoryId = document.CategoryId,
                    CategoryName = document.Category.CategoryName,
                    StatusId = document.StatusId,
                    StatusName = document.Status.StatusName,
                    StatusCode = document.Status.StatusCode,
                    Priority = document.Priority,
                    PriorityText = GetPriorityText(document.Priority),
                    FileName = document.FileName,
                    FileSize = document.FileSize,
                    CurrentWorkflowLevel = document.CurrentWorkflowLevel,
                    CreatedByUserId = document.CreatedByUserId,
                    CreatedByName = document.CreatedByUser.FullName,
                    CurrentHandlerUserId = document.CurrentHandlerUserId,
                    CurrentHandlerName = document.CurrentHandlerUser?.FullName,
                    DepartmentId = document.DepartmentId,
                    DepartmentName = document.Department?.DepartmentName,
                    DueDate = document.DueDate,
                    CompletedAt = document.CompletedAt,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    CurrentVersion = currentVersion != null ? new DocumentVersionDto
                    {
                        VersionId = currentVersion.VersionId,
                        DocumentId = currentVersion.DocumentId,
                        VersionNumber = currentVersion.VersionNumber,
                        FileName = currentVersion.FileName,
                        FileSize = currentVersion.FileSize,
                        CreatedByUserId = currentVersion.CreatedByUserId,
                        CreatedByName = currentVersion.CreatedByUser.FullName,
                        ChangeDescription = currentVersion.ChangeDescription,
                        IsCurrentVersion = currentVersion.IsCurrentVersion,
                        CreatedAt = currentVersion.CreatedAt
                    } : null,
                    WorkflowHistory = workflowHistory,
                    CurrentAssignment = currentAssignment,
                    CanEdit = await CanUserEditDocument(document, userId),
                    CanSubmit = await CanUserSubmitDocument(document, userId),
                    CanApprove = await CanUserApproveDocument(document, userId),
                    CanReject = await CanUserRejectDocument(document, userId),
                    CanRequestRevision = await CanUserRequestRevision(document, userId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document detail");
                throw;
            }
        }

        public async Task<DocumentDto?> CreateDocumentAsync(int userId, CreateDocumentRequest request, IFormFile file)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var user = await _unitOfWork.Users.QueryWithIncludes(u => u.Role, u => u.Department!)
                    .SingleOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                // Save file
                var (success, filePath, errorMessage) = await _fileStorageService
                    .SaveFileAsync(file, "documents", null);

                if (!success || filePath == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError("File upload failed: {Error}", errorMessage);
                    return null;
                }

                // Generate document number
                var documentNumber = await _documentNumberGenerator
                    .GenerateDocumentNumberAsync(user.Department?.DepartmentCode ?? "GENERAL");

                // Get DRAFT status
                var draftStatus = await _unitOfWork.DocumentStatuses
                    .SingleAsync(s => s.StatusCode == "DRAFT");

                // Create document
                var document = new Document
                {
                    DocumentNumber = documentNumber,
                    Title = request.Title,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    StatusId = draftStatus.StatusId,
                    Priority = request.Priority,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    CurrentWorkflowLevel = 1,
                    CreatedByUserId = userId,
                    CurrentHandlerUserId = userId,
                    DepartmentId = user.DepartmentId,
                    DueDate = request.DueDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Documents.AddAsync(document);
                await _unitOfWork.SaveChangesAsync();

                // Create initial version
                var initialVersion = new DocumentVersion
                {
                    DocumentId = document.DocumentId,
                    VersionNumber = 1,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    CreatedByUserId = userId,
                    ChangeDescription = "Phiên bản khởi tạo",
                    IsCurrentVersion = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DocumentVersions.AddAsync(initialVersion);

                // Create workflow history
                var createAction = await _unitOfWork.WorkflowActions
                    .SingleAsync(a => a.ActionCode == "CREATE");

                var workflowHistory = new DocumentWorkflowHistory
                {
                    DocumentId = document.DocumentId,
                    VersionId = initialVersion.VersionId,
                    ActionId = createAction.ActionId,
                    FromUserId = userId,
                    ToUserId = userId,
                    PreviousStatusId = null,
                    NewStatusId = draftStatus.StatusId,
                    Comments = "Tài liệu được tạo",
                    ActionAt = DateTime.UtcNow
                };

                await _unitOfWork.DocumentWorkflowHistory.AddAsync(workflowHistory);

                // Audit log
                await _auditService.LogDocumentActionAsync(
                    userId,
                    "CREATE",
                    document.DocumentId,
                    $"Tạo tài liệu: {document.Title}",
                    null
                );

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var createdDocument = await _unitOfWork.Documents.QueryWithIncludes(
                    d => d.Category,
                    d => d.Status,
                    d => d.CreatedByUser,
                    d => d.Department
                ).SingleOrDefaultAsync(d => d.DocumentId == document.DocumentId);

                return MapToDocumentDto(createdDocument!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<DocumentDto?> UpdateDocumentAsync(int documentId, int userId, UpdateDocumentRequest request)
        {
            try
            {
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null) return null;

                if (!await CanUserEditDocument(document, userId))
                    return null;

                var oldTitle = document.Title;
                
                document.Title = request.Title;
                document.Description = request.Description;
                document.CategoryId = request.CategoryId;
                document.Priority = request.Priority;
                document.DueDate = request.DueDate;
                document.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Documents.UpdateAsync(document);

                await _auditService.LogDocumentActionAsync(
                    userId,
                    "UPDATE",
                    documentId,
                    $"Cập nhật tài liệu: {document.Title}",
                    $"{{\"oldTitle\":\"{oldTitle}\",\"newTitle\":\"{document.Title}\"}}"
                );

                await _unitOfWork.SaveChangesAsync();

                var updatedDocument = await _unitOfWork.Documents.QueryWithIncludes(
                    d => d.Category,
                    d => d.Status,
                    d => d.CreatedByUser,
                    d => d.CurrentHandlerUser!,
                    d => d.Department
                ).SingleOrDefaultAsync(d => d.DocumentId == documentId);

                return MapToDocumentDto(updatedDocument!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document");
                throw;
            }
        }

        #endregion

        #region Workflow Operations

        public async Task<bool> SubmitDocumentAsync(int documentId, int userId, SubmitDocumentRequest request)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var document = await _unitOfWork.Documents.QueryWithIncludes(d => d.Status)
                    .SingleOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null || !await CanUserSubmitDocument(document, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var previousStatusId = document.StatusId;
                var pendingStatus = await _unitOfWork.DocumentStatuses
                    .SingleAsync(s => s.StatusCode == "PENDING");

                document.StatusId = pendingStatus.StatusId;
                document.CurrentWorkflowLevel = 2;
                document.CurrentHandlerUserId = request.ToUserId;
                document.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Documents.UpdateAsync(document);

                // Deactivate old assignments
                var oldAssignments = await _unitOfWork.DocumentAssignments
                    .FindAsync(a => a.DocumentId == documentId && a.IsActive);
                foreach (var assignment in oldAssignments)
                {
                    assignment.IsActive = false;
                    await _unitOfWork.DocumentAssignments.UpdateAsync(assignment);
                }

                // Create new assignment
                var newAssignment = new DocumentAssignment
                {
                    DocumentId = documentId,
                    AssignedToUserId = request.ToUserId,
                    AssignedByUserId = userId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _unitOfWork.DocumentAssignments.AddAsync(newAssignment);

                // Workflow history
                var submitAction = await _unitOfWork.WorkflowActions
                    .SingleAsync(a => a.ActionCode == "SUBMIT");

                var workflowHistory = new DocumentWorkflowHistory
                {
                    DocumentId = documentId,
                    ActionId = submitAction.ActionId,
                    FromUserId = userId,
                    ToUserId = request.ToUserId,
                    PreviousStatusId = previousStatusId,
                    NewStatusId = pendingStatus.StatusId,
                    Comments = request.Comments ?? "Trình duyệt tài liệu",
                    ActionAt = DateTime.UtcNow
                };
                await _unitOfWork.DocumentWorkflowHistory.AddAsync(workflowHistory);

                // Notification
                await _notificationService.CreateNotificationAsync(
                    request.ToUserId,
                    "DOC_ASSIGNED",
                    $"Bạn được giao duyệt tài liệu: {document.Title}",
                    documentId
                );

                await _auditService.LogDocumentActionAsync(
                    userId,
                    "SUBMIT",
                    documentId,
                    $"Trình duyệt tài liệu cho user {request.ToUserId}",
                    null
                );

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting document");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> ApproveDocumentAsync(int documentId, int userId, ApproveDocumentRequest request)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var document = await _unitOfWork.Documents.QueryWithIncludes(d => d.Status)
                    .SingleOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null || !await CanUserApproveDocument(document, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var previousStatusId = document.StatusId;
                DocumentStatus newStatus;

                if (request.SendToNextLevel && request.NextLevelUserId.HasValue)
                {
                    // Forward to next level
                    newStatus = await _unitOfWork.DocumentStatuses
                        .SingleAsync(s => s.StatusCode == "IN_REVIEW");

                    document.CurrentWorkflowLevel = 3;
                    document.CurrentHandlerUserId = request.NextLevelUserId.Value;

                    // Create assignment
                    var assignment = new DocumentAssignment
                    {
                        DocumentId = documentId,
                        AssignedToUserId = request.NextLevelUserId.Value,
                        AssignedByUserId = userId,
                        AssignedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await _unitOfWork.DocumentAssignments.AddAsync(assignment);

                    // Notification
                    await _notificationService.CreateNotificationAsync(
                        request.NextLevelUserId.Value,
                        "DOC_ASSIGNED",
                        $"Bạn được giao phê duyệt tài liệu: {document.Title}",
                        documentId
                    );
                }
                else
                {
                    // Final approval
                    newStatus = await _unitOfWork.DocumentStatuses
                        .SingleAsync(s => s.StatusCode == "APPROVED");

                    document.CompletedAt = DateTime.UtcNow;

                    // Notify creator
                    await _notificationService.CreateNotificationAsync(
                        document.CreatedByUserId,
                        "DOC_APPROVED",
                        $"Tài liệu '{document.Title}' đã được phê duyệt",
                        documentId
                    );
                }

                document.StatusId = newStatus.StatusId;
                document.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Documents.UpdateAsync(document);

                // Workflow history
                var approveAction = await _unitOfWork.WorkflowActions
                    .SingleAsync(a => a.ActionCode == "APPROVE");

                var workflowHistory = new DocumentWorkflowHistory
                {
                    DocumentId = documentId,
                    ActionId = approveAction.ActionId,
                    FromUserId = userId,
                    ToUserId = request.NextLevelUserId,
                    PreviousStatusId = previousStatusId,
                    NewStatusId = newStatus.StatusId,
                    Comments = request.Comments ?? "Phê duyệt",
                    ActionAt = DateTime.UtcNow
                };
                await _unitOfWork.DocumentWorkflowHistory.AddAsync(workflowHistory);

                await _auditService.LogDocumentActionAsync(
                    userId,
                    "APPROVE",
                    documentId,
                    $"Phê duyệt tài liệu",
                    null
                );

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RejectDocumentAsync(int documentId, int userId, RejectDocumentRequest request)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null || !await CanUserRejectDocument(document, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var previousStatusId = document.StatusId;
                var rejectedStatus = await _unitOfWork.DocumentStatuses
                    .SingleAsync(s => s.StatusCode == "REJECTED");

                document.StatusId = rejectedStatus.StatusId;
                document.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Documents.UpdateAsync(document);

                // Workflow history
                var rejectAction = await _unitOfWork.WorkflowActions
                    .SingleAsync(a => a.ActionCode == "REJECT");

                var workflowHistory = new DocumentWorkflowHistory
                {
                    DocumentId = documentId,
                    ActionId = rejectAction.ActionId,
                    FromUserId = userId,
                    ToUserId = document.CreatedByUserId,
                    PreviousStatusId = previousStatusId,
                    NewStatusId = rejectedStatus.StatusId,
                    Comments = request.Comments ?? "Từ chối",
                    ActionAt = DateTime.UtcNow
                };
                await _unitOfWork.DocumentWorkflowHistory.AddAsync(workflowHistory);

                // Notification
                await _notificationService.CreateNotificationAsync(
                    document.CreatedByUserId,
                    "DOC_REJECTED",
                    $"Tài liệu '{document.Title}' đã bị từ chối. Lý do: {request.Comments}",
                    documentId
                );

                await _auditService.LogDocumentActionAsync(
                    userId,
                    "REJECT",
                    documentId,
                    $"Từ chối tài liệu",
                    null
                );

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RequestRevisionAsync(int documentId, int userId, RequestRevisionRequest request)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null || !await CanUserRequestRevision(document, userId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var previousStatusId = document.StatusId;
                var revisionStatus = await _unitOfWork.DocumentStatuses
                    .SingleAsync(s => s.StatusCode == "REVISION_REQUESTED");

                document.StatusId = revisionStatus.StatusId;
                document.CurrentWorkflowLevel = 1;
                document.CurrentHandlerUserId = request.SendBackToUserId;
                document.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Documents.UpdateAsync(document);

                // Workflow history
                var revisionAction = await _unitOfWork.WorkflowActions
                    .SingleAsync(a => a.ActionCode == "REQUEST_REVISION");

                var workflowHistory = new DocumentWorkflowHistory
                {
                    DocumentId = documentId,
                    ActionId = revisionAction.ActionId,
                    FromUserId = userId,
                    ToUserId = request.SendBackToUserId,
                    PreviousStatusId = previousStatusId,
                    NewStatusId = revisionStatus.StatusId,
                    Comments = request.Comments ?? "Yêu cầu chỉnh sửa",
                    ActionAt = DateTime.UtcNow
                };
                await _unitOfWork.DocumentWorkflowHistory.AddAsync(workflowHistory);

                // Notification
                await _notificationService.CreateNotificationAsync(
                    request.SendBackToUserId,
                    "DOC_REVISION_REQUESTED",
                    $"Tài liệu '{document.Title}' yêu cầu chỉnh sửa: {request.Comments}",
                    documentId
                );

                await _auditService.LogDocumentActionAsync(
                    userId,
                    "REQUEST_REVISION",
                    documentId,
                    $"Yêu cầu chỉnh sửa tài liệu",
                    null
                );

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting revision");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Version Management

        public async Task<DocumentVersionDto?> CreateDocumentVersionAsync(int documentId, int userId, IFormFile file, string? changeDescription)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                var user = await _unitOfWork.Users.QueryWithIncludes(u => u.Role)
                    .SingleOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                var maxVersion = await _unitOfWork.DocumentVersions.Query()
                    .Where(v => v.DocumentId == documentId)
                    .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

                var nextVersionNumber = maxVersion + 1;

                var (success, filePath, errorMessage) = await _fileStorageService
                    .SaveFileAsync(file, "documents", null);

                if (!success || filePath == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return null;
                }

                // Mark all versions as not current
                var allVersions = await _unitOfWork.DocumentVersions
                    .FindAsync(v => v.DocumentId == documentId && v.IsCurrentVersion);

                foreach (var v in allVersions)
                {
                    v.IsCurrentVersion = false;
                    await _unitOfWork.DocumentVersions.UpdateAsync(v);
                }

                var newVersion = new DocumentVersion
                {
                    DocumentId = documentId,
                    VersionNumber = nextVersionNumber,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    CreatedByUserId = userId,
                    ChangeDescription = changeDescription ?? $"Phiên bản {nextVersionNumber}",
                    IsCurrentVersion = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DocumentVersions.AddAsync(newVersion);

                document.FileName = file.FileName;
                document.FilePath = filePath;
                document.FileSize = file.Length;
                document.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Documents.UpdateAsync(document);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new DocumentVersionDto
                {
                    VersionId = newVersion.VersionId,
                    DocumentId = documentId,
                    VersionNumber = nextVersionNumber,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    CreatedByUserId = userId,
                    CreatedByName = user.FullName,
                    ChangeDescription = changeDescription,
                    IsCurrentVersion = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document version");
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(int documentId)
        {
            try
            {
                var versions = await _unitOfWork.DocumentVersions
                    .QueryWithIncludes(v => v.CreatedByUser)
                    .Where(v => v.DocumentId == documentId)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToListAsync();

                return versions.Select(v => new DocumentVersionDto
                {
                    VersionId = v.VersionId,
                    DocumentId = v.DocumentId,
                    VersionNumber = v.VersionNumber,
                    FileName = v.FileName,
                    FileSize = v.FileSize,
                    CreatedByUserId = v.CreatedByUserId,
                    CreatedByName = v.CreatedByUser.FullName,
                    ChangeDescription = v.ChangeDescription,
                    IsCurrentVersion = v.IsCurrentVersion,
                    CreatedAt = v.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions");
                throw;
            }
        }

        #endregion

        #region Comments

        public async Task<DocumentCommentDto?> AddCommentAsync(int documentId, int userId, AddCommentRequest request)
        {
            try
            {
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null) return null;

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) return null;

                var comment = new DocumentComment
                {
                    DocumentId = documentId,
                    UserId = userId,
                    CommentText = request.CommentText,
                    ParentCommentId = request.ParentCommentId,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DocumentComments.AddAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                // Notify document owner if different
                if (document.CreatedByUserId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        document.CreatedByUserId,
                        "NEW_COMMENT",
                        $"{user.FullName} đã bình luận về tài liệu: {document.Title}",
                        documentId,
                        comment.CommentId
                    );
                }

                return new DocumentCommentDto
                {
                    CommentId = comment.CommentId,
                    DocumentId = documentId,
                    UserId = userId,
                    UserName = user.FullName,
                    CommentText = request.CommentText,
                    ParentCommentId = request.ParentCommentId,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow,
                    Replies = new List<DocumentCommentDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                throw;
            }
        }

        public async Task<List<DocumentCommentDto>> GetDocumentCommentsAsync(int documentId)
        {
            try
            {
                var comments = await _unitOfWork.DocumentComments
                    .QueryWithIncludes(c => c.User, c => c.ResolvedByUser!)
                    .Where(c => c.DocumentId == documentId && c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var commentDtos = new List<DocumentCommentDto>();

                foreach (var comment in comments)
                {
                    var dto = new DocumentCommentDto
                    {
                        CommentId = comment.CommentId,
                        DocumentId = comment.DocumentId,
                        UserId = comment.UserId,
                        UserName = comment.User.FullName,
                        CommentText = comment.CommentText,
                        ParentCommentId = comment.ParentCommentId,
                        IsResolved = comment.IsResolved,
                        ResolvedByName = comment.ResolvedByUser?.FullName,
                        ResolvedAt = comment.ResolvedAt,
                        CreatedAt = comment.CreatedAt,
                        Replies = new List<DocumentCommentDto>()
                    };

                    var replies = await _unitOfWork.DocumentComments
                        .QueryWithIncludes(c => c.User)
                        .Where(c => c.ParentCommentId == comment.CommentId)
                        .OrderBy(c => c.CreatedAt)
                        .ToListAsync();

                    dto.Replies = replies.Select(r => new DocumentCommentDto
                    {
                        CommentId = r.CommentId,
                        DocumentId = r.DocumentId,
                        UserId = r.UserId,
                        UserName = r.User.FullName,
                        CommentText = r.CommentText,
                        ParentCommentId = r.ParentCommentId,
                        IsResolved = r.IsResolved,
                        CreatedAt = r.CreatedAt,
                        Replies = new List<DocumentCommentDto>()
                    }).ToList();

                    commentDtos.Add(dto);
                }

                return commentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments");
                throw;
            }
        }

        #endregion

        #region File Operations

        public async Task<(bool Success, byte[]? FileBytes, string? FileName, string? ContentType)>
            GetDocumentFileAsync(int documentId, int userId, int? versionId = null)
        {
            try
            {
                var document = await _unitOfWork.Documents.GetByIdAsync(documentId);
                if (document == null) return (false, null, null, null);

                // Check access
                if (!await CanUserAccessDocument(document, userId))
                    return (false, null, null, null);

                string? filePath;
                string? fileName;

                if (versionId.HasValue)
                {
                    var version = await _unitOfWork.DocumentVersions.GetByIdAsync(versionId.Value);
                    if (version == null || version.DocumentId != documentId)
                        return (false, null, null, null);

                    filePath = version.FilePath;
                    fileName = version.FileName;
                }
                else
                {
                    filePath = document.FilePath;
                    fileName = document.FileName;
                }

                var (success, fileBytes, errorMessage) = await _fileStorageService.GetFileAsync(filePath);

                if (!success || fileBytes == null)
                {
                    _logger.LogError("Error retrieving file: {Error}", errorMessage);
                    return (false, null, null, null);
                }

                var contentType = GetContentType(fileName);
                return (true, fileBytes, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file");
                return (false, null, null, null);
            }
        }

        #endregion

        #region Lookup Data

        public async Task<List<DocumentCategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.DocumentCategories
                    .Query()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                return categories.Select(c => new DocumentCategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryCode = c.CategoryCode,
                    Description = c.Description,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<List<DocumentStatusDto>> GetStatusesAsync()
        {
            try
            {
                var statuses = await _unitOfWork.DocumentStatuses
                    .Query()
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync();

                return statuses.Select(s => new DocumentStatusDto
                {
                    StatusId = s.StatusId,
                    StatusName = s.StatusName,
                    StatusCode = s.StatusCode,
                    Description = s.Description,
                    DisplayOrder = s.DisplayOrder
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statuses");
                throw;
            }
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users
                    .QueryWithIncludes(u => u.Role)
                    .SingleOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return new DashboardStatsDto();

                var departmentDocuments = _unitOfWork.Documents
                    .Query()
                    .Where(d => d.DepartmentId == user.DepartmentId);

                if (user.Role.RoleLevel == 1)
                {
                    departmentDocuments = departmentDocuments.Where(d =>
                        d.CreatedByUserId == userId || d.CurrentHandlerUserId == userId);
                }
                else if (user.Role.RoleLevel == 2)
                {
                    departmentDocuments = departmentDocuments.Where(d =>
                        d.CreatedByUserId == userId ||
                        d.CurrentHandlerUserId == userId ||
                        d.CurrentWorkflowLevel >= 2);
                }

                var totalDocuments = await departmentDocuments.CountAsync();

                var pendingAssignments = await _unitOfWork.DocumentAssignments
                    .CountAsync(a => a.AssignedToUserId == userId && a.IsActive);

                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var documentsThisMonth = await departmentDocuments
                    .CountAsync(d => d.CreatedAt >= startOfMonth);

                var overdueDocuments = await departmentDocuments
                    .CountAsync(d =>
                        d.CurrentHandlerUserId == userId &&
                        d.DueDate.HasValue &&
                        d.DueDate.Value < DateTime.Now &&
                        d.CompletedAt == null);

                var documentsByStatus = await departmentDocuments
                    .GroupBy(d => new { d.Status.StatusName, d.Status.StatusCode })
                    .Select(g => new StatusStatDto
                    {
                        StatusName = g.Key.StatusName,
                        StatusCode = g.Key.StatusCode,
                        Count = g.Count()
                    })
                    .OrderBy(s => s.StatusName)
                    .ToListAsync();

                return new DashboardStatsDto
                {
                    TotalDocuments = totalDocuments,
                    PendingAssignments = pendingAssignments,
                    DocumentsCreatedThisMonth = documentsThisMonth,
                    OverdueDocuments = overdueDocuments,
                    DocumentsByStatus = documentsByStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private DocumentDto MapToDocumentDto(Document document)
        {
            return new DocumentDto
            {
                DocumentId = document.DocumentId,
                DocumentNumber = document.DocumentNumber,
                Title = document.Title,
                Description = document.Description,
                CategoryId = document.CategoryId,
                CategoryName = document.Category.CategoryName,
                StatusId = document.StatusId,
                StatusName = document.Status.StatusName,
                StatusCode = document.Status.StatusCode,
                Priority = document.Priority,
                PriorityText = GetPriorityText(document.Priority),
                FileName = document.FileName,
                FileSize = document.FileSize,
                CurrentWorkflowLevel = document.CurrentWorkflowLevel,
                CreatedByUserId = document.CreatedByUserId,
                CreatedByName = document.CreatedByUser.FullName,
                CurrentHandlerUserId = document.CurrentHandlerUserId,
                CurrentHandlerName = document.CurrentHandlerUser?.FullName,
                DepartmentId = document.DepartmentId,
                DepartmentName = document.Department?.DepartmentName,
                DueDate = document.DueDate,
                CompletedAt = document.CompletedAt,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }

        private async Task<bool> CanUserAccessDocument(Document document, int userId)
        {
            var user = await _unitOfWork.Users.QueryWithIncludes(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            // Admin can access all
            if (user.Role.RoleLevel == 4) return true;

            // Check department
            if (document.DepartmentId != user.DepartmentId) return false;

            // Trợ lý only sees own documents
            if (user.Role.RoleLevel == 1)
                return document.CreatedByUserId == userId || document.CurrentHandlerUserId == userId;

            // Phó phòng and above can see department documents
            return true;
        }

        private async Task<bool> CanUserEditDocument(Document document, int userId)
        {
            if (document.StatusId != 1) return false; // Only DRAFT

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return document.CreatedByUserId == userId;
        }

        private async Task<bool> CanUserSubmitDocument(Document document, int userId)
        {
            if (document.StatusId != 1) return false; // Only DRAFT
            return document.CreatedByUserId == userId;
        }

        private async Task<bool> CanUserApproveDocument(Document document, int userId)
        {
            if (document.CurrentHandlerUserId != userId) return false;

            var user = await _unitOfWork.Users.QueryWithIncludes(u => u.Role)
                .SingleOrDefaultAsync(u => u.UserId == userId);

            return user != null && user.Role.RoleLevel >= 2;
        }

        private async Task<bool> CanUserRejectDocument(Document document, int userId)
        {
            return await CanUserApproveDocument(document, userId);
        }

        private async Task<bool> CanUserRequestRevision(Document document, int userId)
        {
            return await CanUserApproveDocument(document, userId);
        }

        private string GetPriorityText(int priority)
        {
            return priority switch
            {
                1 => "Cao",
                2 => "Trung bình",
                3 => "Thấp",
                _ => "Không xác định"
            };
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };
        }

        private async Task<List<WorkflowHistoryDto>> GetWorkflowHistoryAsync(int documentId)
        {
            var history = await _unitOfWork.DocumentWorkflowHistory
                .QueryWithIncludes(
                    h => h.Action,
                    h => h.FromUser,
                    h => h.ToUser!,
                    h => h.PreviousStatus!,
                    h => h.NewStatus
                )
                .Where(h => h.DocumentId == documentId)
                .OrderByDescending(h => h.ActionAt)
                .ToListAsync();

            return history.Select(h => new WorkflowHistoryDto
            {
                HistoryId = h.HistoryId,
                ActionName = h.Action.ActionName,
                ActionCode = h.Action.ActionCode,
                FromUserName = h.FromUser.FullName,
                ToUserName = h.ToUser?.FullName,
                PreviousStatus = h.PreviousStatus?.StatusName,
                NewStatus = h.NewStatus.StatusName,
                Comments = h.Comments,
                ActionAt = h.ActionAt
            }).ToList();
        }

        private async Task<DocumentAssignmentDto?> GetCurrentAssignmentAsync(int documentId)
        {
            var assignment = await _unitOfWork.DocumentAssignments
                .QueryWithIncludes(a => a.AssignedToUser, a => a.AssignedByUser)
                .Where(a => a.DocumentId == documentId && a.IsActive)
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();

            if (assignment == null) return null;

            return new DocumentAssignmentDto
            {
                AssignmentId = assignment.AssignmentId,
                DocumentId = assignment.DocumentId,
                AssignedToUserId = assignment.AssignedToUserId,
                AssignedToName = assignment.AssignedToUser.FullName,
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedByName = assignment.AssignedByUser.FullName,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };
        }

        #endregion
    }
}