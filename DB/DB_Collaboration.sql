-- =============================================
-- Document Collaboration System Database Schema
-- =============================================

USE master;
GO

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DocumentCollaborationDB')
BEGIN
    CREATE DATABASE DocumentCollaborationDB;
END
GO

USE DocumentCollaborationDB;
GO

-- =============================================
-- 1. USERS & AUTHENTICATION
-- =============================================

-- User Roles/Levels
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    RoleLevel INT NOT NULL, -- 1: Trợ lý, 2: Phó phòng, 3: Trưởng phòng
    Description NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Users Table
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    DepartmentId INT,
    IsActive BIT DEFAULT 1,
    LastLoginAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- Departments
CREATE TABLE Departments (
    DepartmentId INT PRIMARY KEY IDENTITY(1,1),
    DepartmentName NVARCHAR(255) NOT NULL,
    DepartmentCode NVARCHAR(50) NOT NULL UNIQUE,
    ManagerUserId INT,
    ViceManagerUserId INT,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Departments_Manager FOREIGN KEY (ManagerUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Departments_ViceManager FOREIGN KEY (ViceManagerUserId) REFERENCES Users(UserId)
);

-- Add FK constraint after Departments table is created
ALTER TABLE Users 
ADD CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId);

-- Refresh Tokens for JWT
CREATE TABLE RefreshTokens (
    TokenId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    RevokedAt DATETIME2,
    IsRevoked BIT DEFAULT 0,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =============================================
-- 2. DOCUMENTS & WORKFLOW
-- =============================================

-- Document Categories
CREATE TABLE DocumentCategories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(255) NOT NULL,
    CategoryCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Document Status
CREATE TABLE DocumentStatuses (
    StatusId INT PRIMARY KEY IDENTITY(1,1),
    StatusName NVARCHAR(100) NOT NULL UNIQUE, -- Draft, Pending, Approved, Rejected, Revision
    StatusCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    DisplayOrder INT DEFAULT 0
);

-- Main Documents Table
CREATE TABLE Documents (
    DocumentId INT PRIMARY KEY IDENTITY(1,1),
    DocumentNumber NVARCHAR(100) NOT NULL UNIQUE, -- Auto-generated: DOC-YYYYMMDD-XXXX
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    CategoryId INT NOT NULL,
    StatusId INT NOT NULL,
    
    -- Creator (usually Assistant)
    CreatedByUserId INT NOT NULL,
    
    -- Current handler
    CurrentHandlerUserId INT,
    
    -- Workflow tracking
    CurrentWorkflowLevel INT DEFAULT 1, -- 1: Assistant, 2: Vice Manager, 3: Manager
    
    -- File information
    FileName NVARCHAR(255) NOT NULL,
    FileExtension NVARCHAR(10) NOT NULL, -- .docx, .xlsx, .pptx
    FilePath NVARCHAR(1000) NOT NULL, -- Physical file path
    FileSize BIGINT, -- in bytes
    CollaboraFileId NVARCHAR(255), -- Collabora Online file identifier
    
    -- Metadata
    DepartmentId INT NOT NULL,
    Priority INT DEFAULT 2, -- 1: High, 2: Medium, 3: Low
    DueDate DATETIME2,
    
    -- Timestamps
    SubmittedAt DATETIME2,
    ApprovedAt DATETIME2,
    RejectedAt DATETIME2,
    CompletedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_Documents_Categories FOREIGN KEY (CategoryId) REFERENCES DocumentCategories(CategoryId),
    CONSTRAINT FK_Documents_Statuses FOREIGN KEY (StatusId) REFERENCES DocumentStatuses(StatusId),
    CONSTRAINT FK_Documents_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Documents_CurrentHandler FOREIGN KEY (CurrentHandlerUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Documents_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId)
);

-- Document Versions (Track all changes)
CREATE TABLE DocumentVersions (
    VersionId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    VersionNumber INT NOT NULL, -- 1, 2, 3, ...
    
    -- File information for this version
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(1000) NOT NULL,
    FileSize BIGINT,
    
    -- Who created this version
    CreatedByUserId INT NOT NULL,
    ChangeDescription NVARCHAR(MAX), -- What was changed
    
    -- Version metadata
    IsCurrentVersion BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_DocumentVersions_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_DocumentVersions_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_DocumentVersions_Number UNIQUE (DocumentId, VersionNumber)
);

-- =============================================
-- 3. WORKFLOW & APPROVAL PROCESS
-- =============================================

-- Workflow Actions
CREATE TABLE WorkflowActions (
    ActionId INT PRIMARY KEY IDENTITY(1,1),
    ActionName NVARCHAR(100) NOT NULL UNIQUE, -- Submit, Approve, Reject, RequestRevision, Reassign
    ActionCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

-- Document Workflow History (Audit Trail)
CREATE TABLE DocumentWorkflowHistory (
    HistoryId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    VersionId INT,
    
    -- Action details
    ActionId INT NOT NULL,
    FromUserId INT NOT NULL, -- Who performed the action
    ToUserId INT, -- Who receives the document (nullable for rejection)
    
    -- Workflow levels
    FromWorkflowLevel INT NOT NULL, -- 1, 2, or 3
    ToWorkflowLevel INT, -- Target level (null if rejected)
    
    -- Comments and notes
    Comments NVARCHAR(MAX),
    
    -- Status before and after
    PreviousStatusId INT,
    NewStatusId INT NOT NULL,
    
    -- Timestamp
    ActionAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_WorkflowHistory_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_WorkflowHistory_Versions FOREIGN KEY (VersionId) REFERENCES DocumentVersions(VersionId),
    CONSTRAINT FK_WorkflowHistory_Actions FOREIGN KEY (ActionId) REFERENCES WorkflowActions(ActionId),
    CONSTRAINT FK_WorkflowHistory_FromUser FOREIGN KEY (FromUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_WorkflowHistory_ToUser FOREIGN KEY (ToUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_WorkflowHistory_PreviousStatus FOREIGN KEY (PreviousStatusId) REFERENCES DocumentStatuses(StatusId),
    CONSTRAINT FK_WorkflowHistory_NewStatus FOREIGN KEY (NewStatusId) REFERENCES DocumentStatuses(StatusId)
);

-- Document Assignments (Current assignments)
CREATE TABLE DocumentAssignments (
    AssignmentId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    AssignedToUserId INT NOT NULL,
    AssignedByUserId INT NOT NULL,
    WorkflowLevel INT NOT NULL, -- 1: Assistant, 2: Vice Manager, 3: Manager
    
    -- Status
    IsActive BIT DEFAULT 1, -- Only one active assignment per document
    DueDate DATETIME2,
    
    -- Timestamps
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    CompletedAt DATETIME2,
    
    CONSTRAINT FK_Assignments_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_Assignments_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Assignments_AssignedBy FOREIGN KEY (AssignedByUserId) REFERENCES Users(UserId)
);

-- =============================================
-- 4. COLLABORA INTEGRATION
-- =============================================

-- Collabora Sessions (Track editing sessions)
CREATE TABLE CollaboraSessions (
    SessionId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    VersionId INT,
    UserId INT NOT NULL,
    
    -- Collabora specific
    CollaboraAccessToken NVARCHAR(500), -- WOPI access token
    CollaboraSessionId NVARCHAR(255), -- Collabora session identifier
    
    -- Session info
    StartedAt DATETIME2 DEFAULT GETDATE(),
    EndedAt DATETIME2,
    LastActivityAt DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    
    -- Access info
    AccessMode NVARCHAR(50), -- Edit, View, Review
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    
    CONSTRAINT FK_CollaboraSessions_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_CollaboraSessions_Versions FOREIGN KEY (VersionId) REFERENCES DocumentVersions(VersionId),
    CONSTRAINT FK_CollaboraSessions_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =============================================
-- 5. COMMENTS & COLLABORATION
-- =============================================

-- Document Comments
CREATE TABLE DocumentComments (
    CommentId INT PRIMARY KEY IDENTITY(1,1),
    DocumentId INT NOT NULL,
    VersionId INT,
    UserId INT NOT NULL,
    
    -- Comment details
    CommentText NVARCHAR(MAX) NOT NULL,
    ParentCommentId INT, -- For nested comments/replies
    
    -- Metadata
    IsResolved BIT DEFAULT 0,
    ResolvedByUserId INT,
    ResolvedAt DATETIME2,
    
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_Comments_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_Comments_Versions FOREIGN KEY (VersionId) REFERENCES DocumentVersions(VersionId),
    CONSTRAINT FK_Comments_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Comments_Parent FOREIGN KEY (ParentCommentId) REFERENCES DocumentComments(CommentId),
    CONSTRAINT FK_Comments_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES Users(UserId)
);

-- =============================================
-- 6. NOTIFICATIONS
-- =============================================

-- Notification Types
CREATE TABLE NotificationTypes (
    NotificationTypeId INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(100) NOT NULL UNIQUE, -- DocumentAssigned, DocumentApproved, DocumentRejected, etc.
    TypeCode NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

-- Notifications
CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL, -- Recipient
    NotificationTypeId INT NOT NULL,
    
    -- Related entities
    DocumentId INT,
    CommentId INT,
    
    -- Notification content
    Title NVARCHAR(255) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Link NVARCHAR(500), -- URL to navigate to
    
    -- Status
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Notifications_Types FOREIGN KEY (NotificationTypeId) REFERENCES NotificationTypes(NotificationTypeId),
    CONSTRAINT FK_Notifications_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(DocumentId),
    CONSTRAINT FK_Notifications_Comments FOREIGN KEY (CommentId) REFERENCES DocumentComments(CommentId)
);

-- =============================================
-- 7. AUDIT TRAIL (System-wide logging)
-- =============================================

-- Audit Logs (Track all system actions)
CREATE TABLE AuditLogs (
    AuditId BIGINT PRIMARY KEY IDENTITY(1,1),
    
    -- User and action
    UserId INT,
    ActionType NVARCHAR(100) NOT NULL, -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT, etc.
    EntityType NVARCHAR(100), -- Document, User, Comment, etc.
    EntityId INT, -- ID of the affected entity
    
    -- Details
    ActionDescription NVARCHAR(MAX),
    OldValue NVARCHAR(MAX), -- JSON of old values
    NewValue NVARCHAR(MAX), -- JSON of new values
    
    -- Request info
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    
    -- Timestamp
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =============================================
-- 8. SYSTEM SETTINGS
-- =============================================

-- System Configuration
CREATE TABLE SystemSettings (
    SettingId INT PRIMARY KEY IDENTITY(1,1),
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX),
    SettingType NVARCHAR(50), -- String, Integer, Boolean, JSON
    Description NVARCHAR(500),
    IsEditable BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- =============================================
-- 9. INDEXES FOR PERFORMANCE
-- =============================================

-- Users indexes
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IX_Users_DepartmentId ON Users(DepartmentId);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);

-- Documents indexes
CREATE INDEX IX_Documents_DocumentNumber ON Documents(DocumentNumber);
CREATE INDEX IX_Documents_StatusId ON Documents(StatusId);
CREATE INDEX IX_Documents_CreatedByUserId ON Documents(CreatedByUserId);
CREATE INDEX IX_Documents_CurrentHandlerUserId ON Documents(CurrentHandlerUserId);
CREATE INDEX IX_Documents_DepartmentId ON Documents(DepartmentId);
CREATE INDEX IX_Documents_CategoryId ON Documents(CategoryId);
CREATE INDEX IX_Documents_CreatedAt ON Documents(CreatedAt);
CREATE INDEX IX_Documents_CurrentWorkflowLevel ON Documents(CurrentWorkflowLevel);

-- DocumentVersions indexes
CREATE INDEX IX_DocumentVersions_DocumentId ON DocumentVersions(DocumentId);
CREATE INDEX IX_DocumentVersions_IsCurrentVersion ON DocumentVersions(IsCurrentVersion);

-- DocumentWorkflowHistory indexes
CREATE INDEX IX_WorkflowHistory_DocumentId ON DocumentWorkflowHistory(DocumentId);
CREATE INDEX IX_WorkflowHistory_FromUserId ON DocumentWorkflowHistory(FromUserId);
CREATE INDEX IX_WorkflowHistory_ToUserId ON DocumentWorkflowHistory(ToUserId);
CREATE INDEX IX_WorkflowHistory_ActionAt ON DocumentWorkflowHistory(ActionAt);

-- DocumentAssignments indexes
CREATE INDEX IX_Assignments_DocumentId ON DocumentAssignments(DocumentId);
CREATE INDEX IX_Assignments_AssignedToUserId ON DocumentAssignments(AssignedToUserId);
CREATE INDEX IX_Assignments_IsActive ON DocumentAssignments(IsActive);

-- CollaboraSessions indexes
CREATE INDEX IX_CollaboraSessions_DocumentId ON CollaboraSessions(DocumentId);
CREATE INDEX IX_CollaboraSessions_UserId ON CollaboraSessions(UserId);
CREATE INDEX IX_CollaboraSessions_IsActive ON CollaboraSessions(IsActive);

-- DocumentComments indexes
CREATE INDEX IX_Comments_DocumentId ON DocumentComments(DocumentId);
CREATE INDEX IX_Comments_UserId ON DocumentComments(UserId);
CREATE INDEX IX_Comments_IsResolved ON DocumentComments(IsResolved);

-- Notifications indexes
CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);
CREATE INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt);

-- AuditLogs indexes
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);
CREATE INDEX IX_AuditLogs_ActionType ON AuditLogs(ActionType);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);

GO

PRINT 'Database schema created successfully!';


------------------------------------------------------------------------------------------------------------------------------------------




------------------------------------------------------------------------------------------------------------------------------------------

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



----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------




----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------