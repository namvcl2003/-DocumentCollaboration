-- =============================================
-- Seed Data for Document Collaboration System
-- =============================================

USE DocumentCollaborationDB;
GO

-- =============================================
-- 1. ROLES
-- =============================================
SET IDENTITY_INSERT Roles ON;

INSERT INTO Roles (RoleId, RoleName, RoleLevel, Description)
VALUES 
    (1, N'Trợ lý', 1, N'Nhân viên trợ lý - soạn thảo văn bản ban đầu'),
    (2, N'Phó phòng', 2, N'Phó trưởng phòng - xem xét và chỉnh sửa văn bản'),
    (3, N'Trưởng phòng', 3, N'Trưởng phòng - phê duyệt cuối cùng'),
    (4, N'Quản trị viên', 4, N'Quản trị hệ thống');

SET IDENTITY_INSERT Roles OFF;

-- =============================================
-- 2. DOCUMENT STATUSES
-- =============================================
SET IDENTITY_INSERT DocumentStatuses ON;

INSERT INTO DocumentStatuses (StatusId, StatusName, StatusCode, Description, DisplayOrder)
VALUES 
    (1, N'Bản thảo', 'DRAFT', N'Văn bản đang được soạn thảo', 1),
    (2, N'Chờ duyệt', 'PENDING', N'Văn bản đã gửi và đang chờ xem xét', 2),
    (3, N'Đang xem xét', 'IN_REVIEW', N'Văn bản đang được xem xét bởi cấp trên', 3),
    (4, N'Yêu cầu chỉnh sửa', 'REVISION_REQUESTED', N'Văn bản cần chỉnh sửa theo yêu cầu', 4),
    (5, N'Đã phê duyệt', 'APPROVED', N'Văn bản đã được phê duyệt', 5),
    (6, N'Từ chối', 'REJECTED', N'Văn bản bị từ chối', 6),
    (7, N'Hoàn thành', 'COMPLETED', N'Quy trình hoàn tất', 7);

SET IDENTITY_INSERT DocumentStatuses OFF;

-- =============================================
-- 3. WORKFLOW ACTIONS
-- =============================================
SET IDENTITY_INSERT WorkflowActions ON;

INSERT INTO WorkflowActions (ActionId, ActionName, ActionCode, Description)
VALUES 
    (1, N'Tạo mới', 'CREATE', N'Tạo văn bản mới'),
    (2, N'Gửi duyệt', 'SUBMIT', N'Gửi văn bản lên cấp trên để xem xét'),
    (3, N'Phê duyệt', 'APPROVE', N'Phê duyệt văn bản'),
    (4, N'Từ chối', 'REJECT', N'Từ chối văn bản'),
    (5, N'Yêu cầu chỉnh sửa', 'REQUEST_REVISION', N'Yêu cầu chỉnh sửa và gửi lại'),
    (6, N'Chỉnh sửa', 'EDIT', N'Chỉnh sửa nội dung văn bản'),
    (7, N'Chuyển tiếp', 'FORWARD', N'Chuyển tiếp văn bản đến người khác'),
    (8, N'Hoàn thành', 'COMPLETE', N'Đánh dấu hoàn thành quy trình');

SET IDENTITY_INSERT WorkflowActions OFF;

-- =============================================
-- 4. DOCUMENT CATEGORIES
-- =============================================
SET IDENTITY_INSERT DocumentCategories ON;

INSERT INTO DocumentCategories (CategoryId, CategoryName, CategoryCode, Description)
VALUES 
    (1, N'Văn bản hành chính', 'VB_HC', N'Các văn bản hành chính nội bộ'),
    (2, N'Báo cáo', 'BAO_CAO', N'Các loại báo cáo'),
    (3, N'Công văn', 'CONG_VAN', N'Công văn đi và đến'),
    (4, N'Quyết định', 'QUYET_DINH', N'Quyết định, quy định'),
    (5, N'Hợp đồng', 'HOP_DONG', N'Hợp đồng, thỏa thuận'),
    (6, N'Thông báo', 'THONG_BAO', N'Thông báo nội bộ'),
    (7, N'Đề xuất', 'DE_XUAT', N'Đề xuất, kiến nghị'),
    (8, N'Khác', 'KHAC', N'Các loại văn bản khác');

SET IDENTITY_INSERT DocumentCategories OFF;

-- =============================================
-- 5. NOTIFICATION TYPES
-- =============================================
SET IDENTITY_INSERT NotificationTypes ON;

INSERT INTO NotificationTypes (NotificationTypeId, TypeName, TypeCode, Description)
VALUES 
    (1, N'Văn bản mới được giao', 'DOC_ASSIGNED', N'Có văn bản mới được giao cho bạn'),
    (2, N'Văn bản được phê duyệt', 'DOC_APPROVED', N'Văn bản của bạn đã được phê duyệt'),
    (3, N'Văn bản bị từ chối', 'DOC_REJECTED', N'Văn bản của bạn bị từ chối'),
    (4, N'Yêu cầu chỉnh sửa', 'DOC_REVISION_REQUESTED', N'Văn bản của bạn cần chỉnh sửa'),
    (5, N'Bình luận mới', 'NEW_COMMENT', N'Có bình luận mới trên văn bản của bạn'),
    (6, N'Nhắc nhở hạn chót', 'DEADLINE_REMINDER', N'Nhắc nhở về hạn chót văn bản'),
    (7, N'Văn bản quá hạn', 'DOC_OVERDUE', N'Văn bản đã quá hạn chót');

SET IDENTITY_INSERT NotificationTypes OFF;

-- =============================================
-- 6. DEPARTMENTS
-- =============================================
SET IDENTITY_INSERT Departments ON;

INSERT INTO Departments (DepartmentId, DepartmentName, DepartmentCode, Description)
VALUES 
    (1, N'Phòng Hành chính', 'HANH_CHINH', N'Phòng hành chính - nhân sự'),
    (2, N'Phòng Kế toán', 'KE_TOAN', N'Phòng kế toán - tài chính'),
    (3, N'Phòng Kinh doanh', 'KINH_DOANH', N'Phòng kinh doanh - marketing'),
    (4, N'Phòng Kỹ thuật', 'KY_THUAT', N'Phòng kỹ thuật - công nghệ thông tin');

SET IDENTITY_INSERT Departments OFF;

-- =============================================
-- 7. SAMPLE USERS (Password: Admin@123)
-- =============================================
-- Note: In production, use proper password hashing (BCrypt, PBKDF2, etc.)
-- This is example hash for "Admin@123" using BCrypt

SET IDENTITY_INSERT Users ON;

INSERT INTO Users (UserId, Username, Email, PasswordHash, FullName, RoleId, DepartmentId, IsActive)
VALUES 
    -- Admin
    (1, 'admin', 'admin@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Nguyễn Văn Admin', 4, 1, 1),
    
    -- Phòng Hành chính
    (2, 'truongphong_hc', 'truongphong.hc@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Trần Thị Mai', 3, 1, 1),
    (3, 'phophong_hc', 'phophong.hc@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Lê Văn Hùng', 2, 1, 1),
    (4, 'troly_hc1', 'troly.hc1@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Phạm Thị Lan', 1, 1, 1),
    (5, 'troly_hc2', 'troly.hc2@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Hoàng Văn Nam', 1, 1, 1),
    
    -- Phòng Kế toán
    (6, 'truongphong_kt', 'truongphong.kt@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Đỗ Thị Hương', 3, 2, 1),
    (7, 'phophong_kt', 'phophong.kt@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Vũ Văn Dũng', 2, 2, 1),
    (8, 'troly_kt1', 'troly.kt1@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Ngô Thị Thu', 1, 2, 1),
    
    -- Phòng Kinh doanh
    (9, 'truongphong_kd', 'truongphong.kd@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Bùi Văn Thắng', 3, 3, 1),
    (10, 'phophong_kd', 'phophong.kd@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Đinh Thị Hoa', 2, 3, 1),
    (11, 'troly_kd1', 'troly.kd1@company.com', '$2a$11$7LWZ3wvVc7xFPRBqX9DLqeBbAcNYLmQp8e5aLYKY6xmZXrqWkBfNK', N'Trịnh Văn An', 1, 3, 1);

SET IDENTITY_INSERT Users OFF;

-- Update Department Managers
UPDATE Departments SET ManagerUserId = 2, ViceManagerUserId = 3 WHERE DepartmentId = 1; -- Hành chính
UPDATE Departments SET ManagerUserId = 6, ViceManagerUserId = 7 WHERE DepartmentId = 2; -- Kế toán
UPDATE Departments SET ManagerUserId = 9, ViceManagerUserId = 10 WHERE DepartmentId = 3; -- Kinh doanh

-- =============================================
-- 8. SYSTEM SETTINGS
-- =============================================
INSERT INTO SystemSettings (SettingKey, SettingValue, SettingType, Description, IsEditable)
VALUES 
    ('DOCUMENT_NUMBER_PREFIX', 'DOC', 'String', N'Tiền tố cho mã văn bản', 1),
    ('MAX_FILE_SIZE_MB', '50', 'Integer', N'Kích thước file tối đa (MB)', 1),
    ('ALLOWED_FILE_EXTENSIONS', '.docx,.xlsx,.pptx,.doc,.xls,.ppt', 'String', N'Các định dạng file được phép', 1),
    ('DEFAULT_DOCUMENT_PRIORITY', '2', 'Integer', N'Độ ưu tiên mặc định (1=Cao, 2=Trung bình, 3=Thấp)', 1),
    ('AUTO_ASSIGN_TO_VICE_MANAGER', 'true', 'Boolean', N'Tự động giao cho phó phòng khi trợ lý gửi', 1),
    ('ENABLE_EMAIL_NOTIFICATIONS', 'true', 'Boolean', N'Bật thông báo qua email', 1),
    ('COLLABORA_SERVER_URL', 'http://localhost:9980', 'String', N'URL của Collabora Online server', 1),
    ('FILE_STORAGE_PATH', 'D:/DocumentStorage', 'String', N'Đường dẫn lưu trữ file', 1),
    ('SESSION_TIMEOUT_MINUTES', '30', 'Integer', N'Thời gian timeout của phiên làm việc (phút)', 1),
    ('REQUIRE_COMMENT_ON_REJECT', 'true', 'Boolean', N'Bắt buộc ghi chú khi từ chối văn bản', 1);

GO

-- =============================================
-- 9. SAMPLE DOCUMENT DATA (Optional)
-- =============================================

-- Create a sample document from Assistant
SET IDENTITY_INSERT Documents ON;

INSERT INTO Documents (
    DocumentId, DocumentNumber, Title, Description, CategoryId, StatusId,
    CreatedByUserId, CurrentHandlerUserId, CurrentWorkflowLevel,
    FileName, FileExtension, FilePath, FileSize, 
    DepartmentId, Priority, CreatedAt
)
VALUES 
    (1, 'DOC-20250101-0001', N'Báo cáo tổng kết năm 2024', 
     N'Báo cáo tổng kết hoạt động của phòng hành chính năm 2024', 
     2, 1, 4, 4, 1, 
     'BaoCaoTongKet2024.docx', '.docx', '/documents/2025/01/BaoCaoTongKet2024.docx', 102400,
     1, 2, GETDATE());

SET IDENTITY_INSERT Documents OFF;

-- Create initial version
SET IDENTITY_INSERT DocumentVersions ON;

INSERT INTO DocumentVersions (
    VersionId, DocumentId, VersionNumber, FileName, FilePath, FileSize,
    CreatedByUserId, ChangeDescription, IsCurrentVersion, CreatedAt
)
VALUES 
    (1, 1, 1, 'BaoCaoTongKet2024.docx', '/documents/2025/01/BaoCaoTongKet2024_v1.docx', 102400,
     4, N'Phiên bản khởi tạo', 1, GETDATE());

SET IDENTITY_INSERT DocumentVersions OFF;

-- Create workflow history for document creation
SET IDENTITY_INSERT DocumentWorkflowHistory ON;

INSERT INTO DocumentWorkflowHistory (
    HistoryId, DocumentId, VersionId, ActionId, FromUserId, ToUserId,
    FromWorkflowLevel, ToWorkflowLevel, Comments, PreviousStatusId, NewStatusId
)
VALUES 
    (1, 1, 1, 1, 4, 4, 1, 1, N'Tạo văn bản mới', NULL, 1);

SET IDENTITY_INSERT DocumentWorkflowHistory OFF;

GO

PRINT 'Seed data inserted successfully!';
PRINT '';
PRINT 'Sample Login Credentials (All passwords: Admin@123):';
PRINT '================================================';
PRINT 'Admin:           admin / admin@company.com';
PRINT '';
PRINT 'Phòng Hành chính:';
PRINT '  Trưởng phòng:  truongphong_hc / truongphong.hc@company.com';
PRINT '  Phó phòng:     phophong_hc / phophong.hc@company.com';
PRINT '  Trợ lý 1:      troly_hc1 / troly.hc1@company.com';
PRINT '  Trợ lý 2:      troly_hc2 / troly.hc2@company.com';
PRINT '';
PRINT 'Phòng Kế toán:';
PRINT '  Trưởng phòng:  truongphong_kt / truongphong.kt@company.com';
PRINT '  Phó phòng:     phophong_kt / phophong.kt@company.com';
PRINT '  Trợ lý:        troly_kt1 / troly.kt1@company.com';
PRINT '';
PRINT 'Phòng Kinh doanh:';
PRINT '  Trưởng phòng:  truongphong_kd / truongphong.kd@company.com';
PRINT '  Phó phòng:     phophong_kd / phophong.kd@company.com';
PRINT '  Trợ lý:        troly_kd1 / troly.kd1@company.com';
PRINT '================================================';
