namespace DocumentCollaboration.Domain.Enums
{
    /// <summary>
    /// User role levels in the system
    /// </summary>
    public enum RoleLevel
    {
        Assistant = 1,      // Trợ lý
        ViceManager = 2,    // Phó phòng
        Manager = 3,        // Trưởng phòng
        Admin = 4           // Quản trị viên
    }

    /// <summary>
    /// Document status codes
    /// </summary>
    public enum DocumentStatusCode
    {
        Draft,              // DRAFT - Bản thảo
        Pending,            // PENDING - Chờ duyệt
        InReview,           // IN_REVIEW - Đang xem xét
        RevisionRequested,  // REVISION_REQUESTED - Yêu cầu chỉnh sửa
        Approved,           // APPROVED - Đã phê duyệt
        Rejected,           // REJECTED - Từ chối
        Completed           // COMPLETED - Hoàn thành
    }

    /// <summary>
    /// Workflow action codes
    /// </summary>
    public enum WorkflowActionCode
    {
        Create,             // CREATE - Tạo mới
        Submit,             // SUBMIT - Gửi duyệt
        Approve,            // APPROVE - Phê duyệt
        Reject,             // REJECT - Từ chối
        RequestRevision,    // REQUEST_REVISION - Yêu cầu chỉnh sửa
        Edit,               // EDIT - Chỉnh sửa
        Forward,            // FORWARD - Chuyển tiếp
        Complete            // COMPLETE - Hoàn thành
    }

    /// <summary>
    /// Document priority levels
    /// </summary>
    public enum DocumentPriority
    {
        High = 1,       // Cao
        Medium = 2,     // Trung bình
        Low = 3         // Thấp
    }

    /// <summary>
    /// Notification type codes
    /// </summary>
    public enum NotificationTypeCode
    {
        DocumentAssigned,       // DOC_ASSIGNED - Văn bản được giao
        DocumentApproved,       // DOC_APPROVED - Văn bản được phê duyệt
        DocumentRejected,       // DOC_REJECTED - Văn bản bị từ chối
        RevisionRequested,      // DOC_REVISION_REQUESTED - Yêu cầu chỉnh sửa
        NewComment,             // NEW_COMMENT - Bình luận mới
        DeadlineReminder,       // DEADLINE_REMINDER - Nhắc nhở hạn chót
        DocumentOverdue         // DOC_OVERDUE - Văn bản quá hạn
    }

    /// <summary>
    /// Collabora access modes
    /// </summary>
    public enum CollaboraAccessMode
    {
        Edit,       // Full editing rights
        View,       // Read-only
        Review      // Can comment but not edit
    }

    /// <summary>
    /// Audit action types
    /// </summary>
    public enum AuditActionType
    {
        Create,
        Update,
        Delete,
        Submit,
        Approve,
        Reject,
        RequestRevision,
        Login,
        Logout,
        VersionCreate,
        CommentCreate,
        AssignmentCreate
    }
}