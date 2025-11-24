using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DocumentCollaboration.Application.DTOs.Documents;
using DocumentCollaboration.Application.Services;

namespace DocumentCollaboration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Get all documents for current user based on their role
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<DocumentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<DocumentDto>>> GetDocuments(
            [FromQuery] DocumentQueryRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var result = await _documentService.GetDocumentsByUserAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh sách văn bản" });
            }
        }

        /// <summary>
        /// Get document details by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentDetailDto>> GetDocumentById(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var result = await _documentService.GetDocumentDetailAsync(id, userId);
                
                if (result == null)
                    return NotFound(new { message = "Không tìm thấy văn bản" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải thông tin văn bản" });
            }
        }

        /// <summary>
        /// Create new document
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentDto>> CreateDocument(
            [FromForm] CreateDocumentRequest request,
            IFormFile file)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng tải lên file văn bản" });

                var result = await _documentService.CreateDocumentAsync(userId, request, file);
                
                if (result == null)
                    return BadRequest(new { message = "Không thể tạo văn bản" });

                _logger.LogInformation("Document created: {DocumentId} by user {UserId}", 
                    result.DocumentId, userId);

                return CreatedAtAction(
                    nameof(GetDocumentById), 
                    new { id = result.DocumentId }, 
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo văn bản" });
            }
        }

        /// <summary>
        /// Update document information (not file)
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentDto>> UpdateDocument(
            int id,
            [FromBody] UpdateDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var result = await _documentService.UpdateDocumentAsync(id, userId, request);
                
                if (result == null)
                    return NotFound(new { message = "Không tìm thấy văn bản hoặc bạn không có quyền chỉnh sửa" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi cập nhật văn bản" });
            }
        }

        /// <summary>
        /// Upload new version of document
        /// </summary>
        [HttpPost("{id}/versions")]
        [ProducesResponseType(typeof(DocumentVersionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentVersionDto>> UploadNewVersion(
            int id,
            IFormFile file,
            [FromForm] string? changeDescription)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng tải lên file" });

                var result = await _documentService.CreateDocumentVersionAsync(
                    id, userId, file, changeDescription);
                
                if (result == null)
                    return BadRequest(new { message = "Không thể tạo phiên bản mới" });

                return CreatedAtAction(nameof(GetDocumentById), new { id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document version for {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo phiên bản mới" });
            }
        }

        /// <summary>
        /// Submit document to next level for review
        /// </summary>
        [HttpPost("{id}/submit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SubmitDocument(
            int id,
            [FromBody] SubmitDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var success = await _documentService.SubmitDocumentAsync(id, userId, request);
                
                if (!success)
                    return BadRequest(new { message = "Không thể gửi văn bản. Vui lòng kiểm tra lại." });

                return Ok(new { message = "Gửi văn bản thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi gửi văn bản" });
            }
        }

        /// <summary>
        /// Approve document
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "RequireViceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ApproveDocument(
            int id,
            [FromBody] ApproveDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var success = await _documentService.ApproveDocumentAsync(id, userId, request);
                
                if (!success)
                    return BadRequest(new { message = "Không thể phê duyệt văn bản" });

                return Ok(new { message = "Phê duyệt văn bản thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi phê duyệt văn bản" });
            }
        }

        /// <summary>
        /// Reject document
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Policy = "RequireViceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> RejectDocument(
            int id,
            [FromBody] RejectDocumentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var success = await _documentService.RejectDocumentAsync(id, userId, request);
                
                if (!success)
                    return BadRequest(new { message = "Không thể từ chối văn bản" });

                return Ok(new { message = "Từ chối văn bản thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi từ chối văn bản" });
            }
        }

        /// <summary>
        /// Request revision for document
        /// </summary>
        [HttpPost("{id}/request-revision")]
        [Authorize(Policy = "RequireViceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> RequestRevision(
            int id,
            [FromBody] RequestRevisionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var success = await _documentService.RequestRevisionAsync(id, userId, request);
                
                if (!success)
                    return BadRequest(new { message = "Không thể yêu cầu chỉnh sửa văn bản" });

                return Ok(new { message = "Yêu cầu chỉnh sửa văn bản thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting revision for document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi yêu cầu chỉnh sửa văn bản" });
            }
        }

        /// <summary>
        /// Add comment to document
        /// </summary>
        [HttpPost("{id}/comments")]
        [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<DocumentCommentDto>> AddComment(
            int id,
            [FromBody] AddCommentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var result = await _documentService.AddCommentAsync(id, userId, request);
                
                if (result == null)
                    return BadRequest(new { message = "Không thể thêm bình luận" });

                return CreatedAtAction(nameof(GetDocumentById), new { id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi thêm bình luận" });
            }
        }

        /// <summary>
        /// Download document file
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DownloadDocument(int id, [FromQuery] int? versionId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var (success, fileBytes, fileName, contentType) = 
                    await _documentService.GetDocumentFileAsync(id, userId, versionId);
                
                if (!success || fileBytes == null || fileName == null)
                    return NotFound(new { message = "Không tìm thấy file" });

                return File(fileBytes, contentType ?? "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải file" });
            }
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        [HttpGet("dashboard/stats")]
        [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Unauthorized();

                var stats = await _documentService.GetDashboardStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải thống kê" });
            }
        }

        /// <summary>
        /// Get document categories
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<DocumentCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentCategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _documentService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải danh mục" });
            }
        }

        /// <summary>
        /// Get document statuses
        /// </summary>
        [HttpGet("statuses")]
        [ProducesResponseType(typeof(List<DocumentStatusDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DocumentStatusDto>>> GetStatuses()
        {
            try
            {
                var statuses = await _documentService.GetStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statuses");
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tải trạng thái" });
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
}