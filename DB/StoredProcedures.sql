-- =============================================
-- Stored Procedures for Document Collaboration System
-- =============================================

USE DocumentCollaborationDB;
GO

-- =============================================
-- 1. USER MANAGEMENT
-- =============================================

-- Get User by Username
CREATE OR ALTER PROCEDURE sp_GetUserByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId, u.Username, u.Email, u.PasswordHash, u.FullName,
        u.RoleId, r.RoleName, r.RoleLevel,
        u.DepartmentId, d.DepartmentName, d.DepartmentCode,
        u.IsActive, u.LastLoginAt, u.CreatedAt, u.UpdatedAt
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    LEFT JOIN Departments d ON u.DepartmentId = d.DepartmentId
    WHERE u.Username = @Username AND u.IsActive = 1;
END
GO

-- Update Last Login
CREATE OR ALTER PROCEDURE sp_UpdateLastLogin
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Users 
    SET LastLoginAt = GETDATE()
    WHERE UserId = @UserId;
END
GO

-- Get Users by Role and Department
CREATE OR ALTER PROCEDURE sp_GetUsersByRoleAndDepartment
    @RoleLevel INT = NULL,
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId, u.Username, u.Email, u.FullName,
        u.RoleId, r.RoleName, r.RoleLevel,
        u.DepartmentId, d.DepartmentName,
        u.IsActive
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    LEFT JOIN Departments d ON u.DepartmentId = d.DepartmentId
    WHERE 
        (@RoleLevel IS NULL OR r.RoleLevel = @RoleLevel)
        AND (@DepartmentId IS NULL OR u.DepartmentId = @DepartmentId)
        AND u.IsActive = 1
    ORDER BY r.RoleLevel, u.FullName;
END
GO

-- =============================================
-- 2. DOCUMENT MANAGEMENT
-- =============================================

-- Create New Document
CREATE OR ALTER PROCEDURE sp_CreateDocument
    @DocumentNumber NVARCHAR(100),
    @Title NVARCHAR(500),
    @Description NVARCHAR(MAX),
    @CategoryId INT,
    @CreatedByUserId INT,
    @DepartmentId INT,
    @FileName NVARCHAR(255),
    @FileExtension NVARCHAR(10),
    @FilePath NVARCHAR(1000),
    @FileSize BIGINT,
    @Priority INT = 2,
    @DueDate DATETIME2 = NULL,
    @DocumentId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get Draft status ID
        DECLARE @DraftStatusId INT;
        SELECT @DraftStatusId = StatusId FROM DocumentStatuses WHERE StatusCode = 'DRAFT';
        
        -- Insert document
        INSERT INTO Documents (
            DocumentNumber, Title, Description, CategoryId, StatusId,
            CreatedByUserId, CurrentHandlerUserId, CurrentWorkflowLevel,
            FileName, FileExtension, FilePath, FileSize,
            DepartmentId, Priority, DueDate, CreatedAt, UpdatedAt
        )
        VALUES (
            @DocumentNumber, @Title, @Description, @CategoryId, @DraftStatusId,
            @CreatedByUserId, @CreatedByUserId, 1,
            @FileName, @FileExtension, @FilePath, @FileSize,
            @DepartmentId, @Priority, @DueDate, GETDATE(), GETDATE()
        );
        
        SET @DocumentId = SCOPE_IDENTITY();
        
        -- Create initial version
        INSERT INTO DocumentVersions (
            DocumentId, VersionNumber, FileName, FilePath, FileSize,
            CreatedByUserId, ChangeDescription, IsCurrentVersion, CreatedAt
        )
        VALUES (
            @DocumentId, 1, @FileName, @FilePath, @FileSize,
            @CreatedByUserId, N'Phiên bản khởi tạo', 1, GETDATE()
        );
        
        DECLARE @VersionId INT = SCOPE_IDENTITY();
        
        -- Log to workflow history
        DECLARE @CreateActionId INT;
        SELECT @CreateActionId = ActionId FROM WorkflowActions WHERE ActionCode = 'CREATE';
        
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId, ToUserId,
            FromWorkflowLevel, ToWorkflowLevel, Comments, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @VersionId, @CreateActionId, @CreatedByUserId, @CreatedByUserId,
            1, 1, N'Tạo văn bản mới', @DraftStatusId, GETDATE()
        );
        
        -- Log to audit trail
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, NewValue, CreatedAt)
        VALUES (
            @CreatedByUserId, 'CREATE', 'Document', @DocumentId,
            N'Tạo văn bản mới: ' + @Title,
            (SELECT * FROM Documents WHERE DocumentId = @DocumentId FOR JSON PATH),
            GETDATE()
        );
        
        COMMIT TRANSACTION;
        
        SELECT @DocumentId AS DocumentId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- Submit Document to Next Level
CREATE OR ALTER PROCEDURE sp_SubmitDocument
    @DocumentId INT,
    @FromUserId INT,
    @ToUserId INT,
    @Comments NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get current document info
        DECLARE @CurrentLevel INT, @CurrentStatusId INT, @CurrentVersionId INT;
        SELECT 
            @CurrentLevel = CurrentWorkflowLevel,
            @CurrentStatusId = StatusId
        FROM Documents WHERE DocumentId = @DocumentId;
        
        -- Get current version
        SELECT @CurrentVersionId = VersionId 
        FROM DocumentVersions 
        WHERE DocumentId = @DocumentId AND IsCurrentVersion = 1;
        
        -- Determine new status
        DECLARE @NewStatusId INT;
        IF @CurrentLevel = 1 -- Assistant submitting
            SELECT @NewStatusId = StatusId FROM DocumentStatuses WHERE StatusCode = 'PENDING';
        ELSE IF @CurrentLevel = 2 -- Vice Manager submitting
            SELECT @NewStatusId = StatusId FROM DocumentStatuses WHERE StatusCode = 'IN_REVIEW';
        
        -- Get new workflow level based on target user's role
        DECLARE @ToUserRoleLevel INT;
        SELECT @ToUserRoleLevel = r.RoleLevel
        FROM Users u
        INNER JOIN Roles r ON u.RoleId = r.RoleId
        WHERE u.UserId = @ToUserId;
        
        -- Update document
        UPDATE Documents
        SET 
            StatusId = @NewStatusId,
            CurrentHandlerUserId = @ToUserId,
            CurrentWorkflowLevel = @ToUserRoleLevel,
            SubmittedAt = GETDATE(),
            UpdatedAt = GETDATE()
        WHERE DocumentId = @DocumentId;
        
        -- Log to workflow history
        DECLARE @SubmitActionId INT;
        SELECT @SubmitActionId = ActionId FROM WorkflowActions WHERE ActionCode = 'SUBMIT';
        
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId, ToUserId,
            FromWorkflowLevel, ToWorkflowLevel, Comments,
            PreviousStatusId, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @CurrentVersionId, @SubmitActionId, @FromUserId, @ToUserId,
            @CurrentLevel, @ToUserRoleLevel, @Comments,
            @CurrentStatusId, @NewStatusId, GETDATE()
        );
        
        -- Create assignment
        INSERT INTO DocumentAssignments (
            DocumentId, AssignedToUserId, AssignedByUserId, 
            WorkflowLevel, IsActive, AssignedAt
        )
        VALUES (
            @DocumentId, @ToUserId, @FromUserId,
            @ToUserRoleLevel, 1, GETDATE()
        );
        
        -- Create notification
        DECLARE @NotificationTypeId INT;
        SELECT @NotificationTypeId = NotificationTypeId 
        FROM NotificationTypes WHERE TypeCode = 'DOC_ASSIGNED';
        
        DECLARE @DocumentTitle NVARCHAR(500);
        SELECT @DocumentTitle = Title FROM Documents WHERE DocumentId = @DocumentId;
        
        INSERT INTO Notifications (
            UserId, NotificationTypeId, DocumentId,
            Title, Message, Link, CreatedAt
        )
        VALUES (
            @ToUserId, @NotificationTypeId, @DocumentId,
            N'Văn bản mới được giao',
            N'Bạn có văn bản mới cần xem xét: ' + @DocumentTitle,
            '/documents/' + CAST(@DocumentId AS NVARCHAR),
            GETDATE()
        );
        
        -- Audit log
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, CreatedAt)
        VALUES (
            @FromUserId, 'SUBMIT', 'Document', @DocumentId,
            N'Gửi văn bản từ cấp ' + CAST(@CurrentLevel AS NVARCHAR) + 
            N' lên cấp ' + CAST(@ToUserRoleLevel AS NVARCHAR),
            GETDATE()
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- Approve Document
CREATE OR ALTER PROCEDURE sp_ApproveDocument
    @DocumentId INT,
    @ApprovedByUserId INT,
    @Comments NVARCHAR(MAX) = NULL,
    @SendToNextLevel BIT = 0, -- If true, send to manager; if false, complete
    @NextLevelUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CurrentStatusId INT, @CurrentLevel INT, @CurrentVersionId INT;
        SELECT 
            @CurrentStatusId = StatusId,
            @CurrentLevel = CurrentWorkflowLevel
        FROM Documents WHERE DocumentId = @DocumentId;
        
        SELECT @CurrentVersionId = VersionId 
        FROM DocumentVersions 
        WHERE DocumentId = @DocumentId AND IsCurrentVersion = 1;
        
        DECLARE @ApproveActionId INT, @NewStatusId INT;
        SELECT @ApproveActionId = ActionId FROM WorkflowActions WHERE ActionCode = 'APPROVE';
        
        IF @SendToNextLevel = 1 AND @NextLevelUserId IS NOT NULL
        BEGIN
            -- Send to next level (e.g., Vice Manager -> Manager)
            SELECT @NewStatusId = StatusId FROM DocumentStatuses WHERE StatusCode = 'IN_REVIEW';
            
            DECLARE @NextUserRoleLevel INT;
            SELECT @NextUserRoleLevel = r.RoleLevel
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId
            WHERE u.UserId = @NextLevelUserId;
            
            UPDATE Documents
            SET 
                StatusId = @NewStatusId,
                CurrentHandlerUserId = @NextLevelUserId,
                CurrentWorkflowLevel = @NextUserRoleLevel,
                UpdatedAt = GETDATE()
            WHERE DocumentId = @DocumentId;
            
            -- Create assignment
            INSERT INTO DocumentAssignments (
                DocumentId, AssignedToUserId, AssignedByUserId,
                WorkflowLevel, IsActive, AssignedAt
            )
            VALUES (
                @DocumentId, @NextLevelUserId, @ApprovedByUserId,
                @NextUserRoleLevel, 1, GETDATE()
            );
            
            -- Notification
            DECLARE @NotificationTypeId INT;
            SELECT @NotificationTypeId = NotificationTypeId 
            FROM NotificationTypes WHERE TypeCode = 'DOC_ASSIGNED';
            
            DECLARE @DocumentTitle NVARCHAR(500);
            SELECT @DocumentTitle = Title FROM Documents WHERE DocumentId = @DocumentId;
            
            INSERT INTO Notifications (
                UserId, NotificationTypeId, DocumentId,
                Title, Message, Link, CreatedAt
            )
            VALUES (
                @NextLevelUserId, @NotificationTypeId, @DocumentId,
                N'Văn bản mới được giao',
                N'Bạn có văn bản cần phê duyệt: ' + @DocumentTitle,
                '/documents/' + CAST(@DocumentId AS NVARCHAR),
                GETDATE()
            );
        END
        ELSE
        BEGIN
            -- Final approval - complete the document
            SELECT @NewStatusId = StatusId FROM DocumentStatuses WHERE StatusCode = 'APPROVED';
            
            UPDATE Documents
            SET 
                StatusId = @NewStatusId,
                ApprovedAt = GETDATE(),
                CompletedAt = GETDATE(),
                UpdatedAt = GETDATE()
            WHERE DocumentId = @DocumentId;
            
            -- Notify document creator
            DECLARE @CreatorUserId INT;
            SELECT @CreatorUserId = CreatedByUserId FROM Documents WHERE DocumentId = @DocumentId;
            
            SET @NotificationTypeId = NULL;
            SELECT @NotificationTypeId = NotificationTypeId 
            FROM NotificationTypes WHERE TypeCode = 'DOC_APPROVED';
            
            SET @DocumentTitle = NULL;
            SELECT @DocumentTitle = Title FROM Documents WHERE DocumentId = @DocumentId;
            
            INSERT INTO Notifications (
                UserId, NotificationTypeId, DocumentId,
                Title, Message, Link, CreatedAt
            )
            VALUES (
                @CreatorUserId, @NotificationTypeId, @DocumentId,
                N'Văn bản đã được phê duyệt',
                N'Văn bản của bạn đã được phê duyệt: ' + @DocumentTitle,
                '/documents/' + CAST(@DocumentId AS NVARCHAR),
                GETDATE()
            );
        END
        
        -- Log to workflow history
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId, ToUserId,
            FromWorkflowLevel, ToWorkflowLevel, Comments,
            PreviousStatusId, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @CurrentVersionId, @ApproveActionId, 
            @ApprovedByUserId, @NextLevelUserId,
            @CurrentLevel, 
            CASE WHEN @SendToNextLevel = 1 THEN @NextUserRoleLevel ELSE NULL END,
            @Comments, @CurrentStatusId, @NewStatusId, GETDATE()
        );
        
        -- Deactivate current assignment
        UPDATE DocumentAssignments
        SET IsActive = 0, CompletedAt = GETDATE()
        WHERE DocumentId = @DocumentId AND IsActive = 1;
        
        -- Audit log
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, CreatedAt)
        VALUES (
            @ApprovedByUserId, 'APPROVE', 'Document', @DocumentId,
            N'Phê duyệt văn bản' + CASE WHEN @SendToNextLevel = 1 THEN N' và chuyển tiếp' ELSE N' (hoàn thành)' END,
            GETDATE()
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- Request Revision
CREATE OR ALTER PROCEDURE sp_RequestRevision
    @DocumentId INT,
    @RequestedByUserId INT,
    @SendBackToUserId INT,
    @Comments NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CurrentStatusId INT, @CurrentLevel INT, @CurrentVersionId INT;
        SELECT 
            @CurrentStatusId = StatusId,
            @CurrentLevel = CurrentWorkflowLevel
        FROM Documents WHERE DocumentId = @DocumentId;
        
        SELECT @CurrentVersionId = VersionId 
        FROM DocumentVersions 
        WHERE DocumentId = @DocumentId AND IsCurrentVersion = 1;
        
        -- Get revision status
        DECLARE @RevisionStatusId INT;
        SELECT @RevisionStatusId = StatusId 
        FROM DocumentStatuses WHERE StatusCode = 'REVISION_REQUESTED';
        
        -- Get target user role level
        DECLARE @TargetUserRoleLevel INT;
        SELECT @TargetUserRoleLevel = r.RoleLevel
        FROM Users u
        INNER JOIN Roles r ON u.RoleId = r.RoleId
        WHERE u.UserId = @SendBackToUserId;
        
        -- Update document
        UPDATE Documents
        SET 
            StatusId = @RevisionStatusId,
            CurrentHandlerUserId = @SendBackToUserId,
            CurrentWorkflowLevel = @TargetUserRoleLevel,
            UpdatedAt = GETDATE()
        WHERE DocumentId = @DocumentId;
        
        -- Log to workflow history
        DECLARE @RevisionActionId INT;
        SELECT @RevisionActionId = ActionId 
        FROM WorkflowActions WHERE ActionCode = 'REQUEST_REVISION';
        
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId, ToUserId,
            FromWorkflowLevel, ToWorkflowLevel, Comments,
            PreviousStatusId, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @CurrentVersionId, @RevisionActionId,
            @RequestedByUserId, @SendBackToUserId,
            @CurrentLevel, @TargetUserRoleLevel, @Comments,
            @CurrentStatusId, @RevisionStatusId, GETDATE()
        );
        
        -- Create assignment
        INSERT INTO DocumentAssignments (
            DocumentId, AssignedToUserId, AssignedByUserId,
            WorkflowLevel, IsActive, AssignedAt
        )
        VALUES (
            @DocumentId, @SendBackToUserId, @RequestedByUserId,
            @TargetUserRoleLevel, 1, GETDATE()
        );
        
        -- Deactivate previous assignment
        UPDATE DocumentAssignments
        SET IsActive = 0, CompletedAt = GETDATE()
        WHERE DocumentId = @DocumentId 
            AND AssignedToUserId = @RequestedByUserId
            AND IsActive = 1;
        
        -- Create notification
        DECLARE @NotificationTypeId INT;
        SELECT @NotificationTypeId = NotificationTypeId 
        FROM NotificationTypes WHERE TypeCode = 'DOC_REVISION_REQUESTED';
        
        DECLARE @DocumentTitle NVARCHAR(500);
        SELECT @DocumentTitle = Title FROM Documents WHERE DocumentId = @DocumentId;
        
        INSERT INTO Notifications (
            UserId, NotificationTypeId, DocumentId,
            Title, Message, Link, CreatedAt
        )
        VALUES (
            @SendBackToUserId, @NotificationTypeId, @DocumentId,
            N'Yêu cầu chỉnh sửa văn bản',
            N'Văn bản cần chỉnh sửa: ' + @DocumentTitle + N'. Ghi chú: ' + @Comments,
            '/documents/' + CAST(@DocumentId AS NVARCHAR),
            GETDATE()
        );
        
        -- Audit log
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, CreatedAt)
        VALUES (
            @RequestedByUserId, 'REQUEST_REVISION', 'Document', @DocumentId,
            N'Yêu cầu chỉnh sửa văn bản',
            GETDATE()
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- Reject Document
CREATE OR ALTER PROCEDURE sp_RejectDocument
    @DocumentId INT,
    @RejectedByUserId INT,
    @Comments NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        DECLARE @CurrentStatusId INT, @CurrentLevel INT, @CurrentVersionId INT;
        SELECT 
            @CurrentStatusId = StatusId,
            @CurrentLevel = CurrentWorkflowLevel
        FROM Documents WHERE DocumentId = @DocumentId;
        
        SELECT @CurrentVersionId = VersionId 
        FROM DocumentVersions 
        WHERE DocumentId = @DocumentId AND IsCurrentVersion = 1;
        
        -- Get rejected status
        DECLARE @RejectedStatusId INT;
        SELECT @RejectedStatusId = StatusId 
        FROM DocumentStatuses WHERE StatusCode = 'REJECTED';
        
        -- Update document
        UPDATE Documents
        SET 
            StatusId = @RejectedStatusId,
            RejectedAt = GETDATE(),
            CompletedAt = GETDATE(),
            UpdatedAt = GETDATE()
        WHERE DocumentId = @DocumentId;
        
        -- Log to workflow history
        DECLARE @RejectActionId INT;
        SELECT @RejectActionId = ActionId FROM WorkflowActions WHERE ActionCode = 'REJECT';
        
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId,
            FromWorkflowLevel, Comments,
            PreviousStatusId, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @CurrentVersionId, @RejectActionId,
            @RejectedByUserId,
            @CurrentLevel, @Comments,
            @CurrentStatusId, @RejectedStatusId, GETDATE()
        );
        
        -- Deactivate assignment
        UPDATE DocumentAssignments
        SET IsActive = 0, CompletedAt = GETDATE()
        WHERE DocumentId = @DocumentId AND IsActive = 1;
        
        -- Notify document creator
        DECLARE @CreatorUserId INT;
        SELECT @CreatorUserId = CreatedByUserId FROM Documents WHERE DocumentId = @DocumentId;
        
        DECLARE @NotificationTypeId INT;
        SELECT @NotificationTypeId = NotificationTypeId 
        FROM NotificationTypes WHERE TypeCode = 'DOC_REJECTED';
        
        DECLARE @DocumentTitle NVARCHAR(500);
        SELECT @DocumentTitle = Title FROM Documents WHERE DocumentId = @DocumentId;
        
        INSERT INTO Notifications (
            UserId, NotificationTypeId, DocumentId,
            Title, Message, Link, CreatedAt
        )
        VALUES (
            @CreatorUserId, @NotificationTypeId, @DocumentId,
            N'Văn bản bị từ chối',
            N'Văn bản của bạn bị từ chối: ' + @DocumentTitle + N'. Lý do: ' + @Comments,
            '/documents/' + CAST(@DocumentId AS NVARCHAR),
            GETDATE()
        );
        
        -- Audit log
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, CreatedAt)
        VALUES (
            @RejectedByUserId, 'REJECT', 'Document', @DocumentId,
            N'Từ chối văn bản: ' + @Comments,
            GETDATE()
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- Create New Document Version
CREATE OR ALTER PROCEDURE sp_CreateDocumentVersion
    @DocumentId INT,
    @FileName NVARCHAR(255),
    @FilePath NVARCHAR(1000),
    @FileSize BIGINT,
    @CreatedByUserId INT,
    @ChangeDescription NVARCHAR(MAX),
    @VersionId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Get next version number
        DECLARE @NextVersionNumber INT;
        SELECT @NextVersionNumber = ISNULL(MAX(VersionNumber), 0) + 1
        FROM DocumentVersions
        WHERE DocumentId = @DocumentId;
        
        -- Mark all previous versions as not current
        UPDATE DocumentVersions
        SET IsCurrentVersion = 0
        WHERE DocumentId = @DocumentId AND IsCurrentVersion = 1;
        
        -- Create new version
        INSERT INTO DocumentVersions (
            DocumentId, VersionNumber, FileName, FilePath, FileSize,
            CreatedByUserId, ChangeDescription, IsCurrentVersion, CreatedAt
        )
        VALUES (
            @DocumentId, @NextVersionNumber, @FileName, @FilePath, @FileSize,
            @CreatedByUserId, @ChangeDescription, 1, GETDATE()
        );
        
        SET @VersionId = SCOPE_IDENTITY();
        
        -- Update document file info
        UPDATE Documents
        SET 
            FileName = @FileName,
            FilePath = @FilePath,
            FileSize = @FileSize,
            UpdatedAt = GETDATE()
        WHERE DocumentId = @DocumentId;
        
        -- Log to workflow history
        DECLARE @EditActionId INT;
        SELECT @EditActionId = ActionId FROM WorkflowActions WHERE ActionCode = 'EDIT';
        
        DECLARE @CurrentStatusId INT;
        SELECT @CurrentStatusId = StatusId FROM Documents WHERE DocumentId = @DocumentId;
        
        INSERT INTO DocumentWorkflowHistory (
            DocumentId, VersionId, ActionId, FromUserId, ToUserId,
            FromWorkflowLevel, ToWorkflowLevel, Comments, NewStatusId, ActionAt
        )
        VALUES (
            @DocumentId, @VersionId, @EditActionId, @CreatedByUserId, @CreatedByUserId,
            (SELECT r.RoleLevel FROM Users u INNER JOIN Roles r ON u.RoleId = r.RoleId WHERE u.UserId = @CreatedByUserId),
            (SELECT r.RoleLevel FROM Users u INNER JOIN Roles r ON u.RoleId = r.RoleId WHERE u.UserId = @CreatedByUserId),
            @ChangeDescription, @CurrentStatusId, GETDATE()
        );
        
        -- Audit log
        INSERT INTO AuditLogs (UserId, ActionType, EntityType, EntityId, ActionDescription, CreatedAt)
        VALUES (
            @CreatedByUserId, 'VERSION_CREATE', 'Document', @DocumentId,
            N'Tạo phiên bản mới (v' + CAST(@NextVersionNumber AS NVARCHAR) + N'): ' + @ChangeDescription,
            GETDATE()
        );
        
        COMMIT TRANSACTION;
        
        SELECT @VersionId AS VersionId, @NextVersionNumber AS VersionNumber;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
GO

-- =============================================
-- 3. QUERY PROCEDURES
-- =============================================

-- Get Documents by User (Based on role and workflow level)
CREATE OR ALTER PROCEDURE sp_GetDocumentsByUser
    @UserId INT,
    @StatusFilter NVARCHAR(50) = NULL, -- 'DRAFT', 'PENDING', 'APPROVED', etc.
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RoleLevel INT, @DepartmentId INT;
    SELECT @RoleLevel = r.RoleLevel, @DepartmentId = u.DepartmentId
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
    
    SELECT 
        d.DocumentId, d.DocumentNumber, d.Title, d.Description,
        d.CategoryId, cat.CategoryName,
        d.StatusId, st.StatusName, st.StatusCode,
        d.CreatedByUserId, creator.FullName AS CreatedByName,
        d.CurrentHandlerUserId, handler.FullName AS CurrentHandlerName,
        d.CurrentWorkflowLevel, d.Priority,
        d.FileName, d.FileExtension, d.FileSize,
        d.DueDate, d.SubmittedAt, d.ApprovedAt, d.RejectedAt,
        d.CreatedAt, d.UpdatedAt,
        -- Assignment info
        da.AssignmentId, da.AssignedAt, da.DueDate AS AssignmentDueDate,
        -- Version info
        dv.VersionNumber AS CurrentVersion
    FROM Documents d
    INNER JOIN DocumentCategories cat ON d.CategoryId = cat.CategoryId
    INNER JOIN DocumentStatuses st ON d.StatusId = st.StatusId
    INNER JOIN Users creator ON d.CreatedByUserId = creator.UserId
    LEFT JOIN Users handler ON d.CurrentHandlerUserId = handler.UserId
    LEFT JOIN DocumentAssignments da ON d.DocumentId = da.DocumentId AND da.IsActive = 1
    LEFT JOIN DocumentVersions dv ON d.DocumentId = dv.DocumentId AND dv.IsCurrentVersion = 1
    WHERE 
        d.DepartmentId = @DepartmentId
        AND (
            -- User's own documents
            d.CreatedByUserId = @UserId
            -- OR documents assigned to user
            OR d.CurrentHandlerUserId = @UserId
            -- OR documents at user's level or below (managers can see all)
            OR (@RoleLevel = 3 AND d.CurrentWorkflowLevel <= 3)
        )
        AND (@StatusFilter IS NULL OR st.StatusCode = @StatusFilter)
    ORDER BY 
        CASE WHEN d.CurrentHandlerUserId = @UserId THEN 0 ELSE 1 END,
        d.Priority,
        d.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    -- Get total count
    SELECT COUNT(*) AS TotalCount
    FROM Documents d
    INNER JOIN DocumentStatuses st ON d.StatusId = st.StatusId
    WHERE 
        d.DepartmentId = @DepartmentId
        AND (
            d.CreatedByUserId = @UserId
            OR d.CurrentHandlerUserId = @UserId
            OR (@RoleLevel = 3 AND d.CurrentWorkflowLevel <= 3)
        )
        AND (@StatusFilter IS NULL OR st.StatusCode = @StatusFilter);
END
GO

-- Get Document Details with Full History
CREATE OR ALTER PROCEDURE sp_GetDocumentDetails
    @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Document info
    SELECT 
        d.DocumentId, d.DocumentNumber, d.Title, d.Description,
        d.CategoryId, cat.CategoryName, cat.CategoryCode,
        d.StatusId, st.StatusName, st.StatusCode,
        d.CreatedByUserId, creator.FullName AS CreatedByName, creator.Email AS CreatedByEmail,
        d.CurrentHandlerUserId, handler.FullName AS CurrentHandlerName,
        d.CurrentWorkflowLevel, d.Priority,
        d.FileName, d.FileExtension, d.FilePath, d.FileSize,
        d.DepartmentId, dept.DepartmentName,
        d.DueDate, d.SubmittedAt, d.ApprovedAt, d.RejectedAt, d.CompletedAt,
        d.CreatedAt, d.UpdatedAt
    FROM Documents d
    INNER JOIN DocumentCategories cat ON d.CategoryId = cat.CategoryId
    INNER JOIN DocumentStatuses st ON d.StatusId = st.StatusId
    INNER JOIN Users creator ON d.CreatedByUserId = creator.UserId
    LEFT JOIN Users handler ON d.CurrentHandlerUserId = handler.UserId
    INNER JOIN Departments dept ON d.DepartmentId = dept.DepartmentId
    WHERE d.DocumentId = @DocumentId;
    
    -- Versions
    SELECT 
        dv.VersionId, dv.VersionNumber, dv.FileName, dv.FilePath, dv.FileSize,
        dv.CreatedByUserId, u.FullName AS CreatedByName,
        dv.ChangeDescription, dv.IsCurrentVersion, dv.CreatedAt
    FROM DocumentVersions dv
    INNER JOIN Users u ON dv.CreatedByUserId = u.UserId
    WHERE dv.DocumentId = @DocumentId
    ORDER BY dv.VersionNumber DESC;
    
    -- Workflow history
    SELECT 
        dwh.HistoryId, dwh.ActionId, wa.ActionName, wa.ActionCode,
        dwh.FromUserId, fromUser.FullName AS FromUserName,
        dwh.ToUserId, toUser.FullName AS ToUserName,
        dwh.FromWorkflowLevel, dwh.ToWorkflowLevel,
        dwh.Comments,
        dwh.PreviousStatusId, prevStatus.StatusName AS PreviousStatusName,
        dwh.NewStatusId, newStatus.StatusName AS NewStatusName,
        dwh.ActionAt
    FROM DocumentWorkflowHistory dwh
    INNER JOIN WorkflowActions wa ON dwh.ActionId = wa.ActionId
    INNER JOIN Users fromUser ON dwh.FromUserId = fromUser.UserId
    LEFT JOIN Users toUser ON dwh.ToUserId = toUser.UserId
    LEFT JOIN DocumentStatuses prevStatus ON dwh.PreviousStatusId = prevStatus.StatusId
    INNER JOIN DocumentStatuses newStatus ON dwh.NewStatusId = newStatus.StatusId
    WHERE dwh.DocumentId = @DocumentId
    ORDER BY dwh.ActionAt DESC;
    
    -- Comments
    SELECT 
        dc.CommentId, dc.CommentText, dc.ParentCommentId,
        dc.UserId, u.FullName AS UserName,
        dc.IsResolved, dc.ResolvedByUserId, resolver.FullName AS ResolvedByName,
        dc.ResolvedAt, dc.CreatedAt, dc.UpdatedAt
    FROM DocumentComments dc
    INNER JOIN Users u ON dc.UserId = u.UserId
    LEFT JOIN Users resolver ON dc.ResolvedByUserId = resolver.UserId
    WHERE dc.DocumentId = @DocumentId
    ORDER BY dc.CreatedAt DESC;
END
GO

-- Get User's Notifications
CREATE OR ALTER PROCEDURE sp_GetUserNotifications
    @UserId INT,
    @IsReadFilter BIT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        n.NotificationId, n.NotificationTypeId, nt.TypeName,
        n.DocumentId, n.CommentId,
        n.Title, n.Message, n.Link,
        n.IsRead, n.ReadAt, n.CreatedAt
    FROM Notifications n
    INNER JOIN NotificationTypes nt ON n.NotificationTypeId = nt.NotificationTypeId
    WHERE 
        n.UserId = @UserId
        AND (@IsReadFilter IS NULL OR n.IsRead = @IsReadFilter)
    ORDER BY n.CreatedAt DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    -- Unread count
    SELECT COUNT(*) AS UnreadCount
    FROM Notifications
    WHERE UserId = @UserId AND IsRead = 0;
END
GO

-- Mark Notification as Read
CREATE OR ALTER PROCEDURE sp_MarkNotificationAsRead
    @NotificationId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE Notifications
    SET IsRead = 1, ReadAt = GETDATE()
    WHERE NotificationId = @NotificationId;
END
GO

-- Get Dashboard Statistics
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RoleLevel INT, @DepartmentId INT;
    SELECT @RoleLevel = r.RoleLevel, @DepartmentId = u.DepartmentId
    FROM Users u
    INNER JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.UserId = @UserId;
    
    -- Documents by status
    SELECT 
        st.StatusName, st.StatusCode,
        COUNT(*) AS Count
    FROM Documents d
    INNER JOIN DocumentStatuses st ON d.StatusId = st.StatusId
    WHERE 
        d.DepartmentId = @DepartmentId
        AND (
            d.CreatedByUserId = @UserId
            OR d.CurrentHandlerUserId = @UserId
            OR (@RoleLevel = 3)
        )
    GROUP BY st.StatusName, st.StatusCode, st.DisplayOrder
    ORDER BY st.DisplayOrder;
    
    -- Pending assignments
    SELECT COUNT(*) AS PendingAssignments
    FROM DocumentAssignments
    WHERE AssignedToUserId = @UserId AND IsActive = 1;
    
    -- Documents created this month
    SELECT COUNT(*) AS DocumentsCreatedThisMonth
    FROM Documents
    WHERE 
        CreatedByUserId = @UserId
        AND YEAR(CreatedAt) = YEAR(GETDATE())
        AND MONTH(CreatedAt) = MONTH(GETDATE());
    
    -- Overdue documents
    SELECT COUNT(*) AS OverdueDocuments
    FROM Documents d
    INNER JOIN DocumentAssignments da ON d.DocumentId = da.DocumentId
    WHERE 
        da.AssignedToUserId = @UserId
        AND da.IsActive = 1
        AND da.DueDate < GETDATE()
        AND d.StatusId NOT IN (SELECT StatusId FROM DocumentStatuses WHERE StatusCode IN ('APPROVED', 'REJECTED', 'COMPLETED'));
END
GO

PRINT 'Stored procedures created successfully!';
