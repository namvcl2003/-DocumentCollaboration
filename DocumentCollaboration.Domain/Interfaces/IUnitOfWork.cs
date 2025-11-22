using System;
using System.Threading.Tasks;
using DocumentCollaboration.Domain.Entities;

namespace DocumentCollaboration.Domain.Interfaces
{
    /// <summary>
    /// Unit of Work pattern for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IRepository<User> Users { get; }
        IRepository<Role> Roles { get; }
        IRepository<Department> Departments { get; }
        IRepository<Document> Documents { get; }
        IRepository<DocumentCategory> DocumentCategories { get; }
        IRepository<DocumentStatus> DocumentStatuses { get; }
        IRepository<DocumentVersion> DocumentVersions { get; }
        IRepository<WorkflowAction> WorkflowActions { get; }
        IRepository<DocumentWorkflowHistory> DocumentWorkflowHistory { get; }
        IRepository<DocumentAssignment> DocumentAssignments { get; }
        IRepository<CollaboraSession> CollaboraSessions { get; }
        IRepository<DocumentComment> DocumentComments { get; }
        IRepository<NotificationType> NotificationTypes { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<AuditLog> AuditLogs { get; }
        IRepository<RefreshToken> RefreshTokens { get; }
        IRepository<SystemSetting> SystemSettings { get; }

        // Transaction management
        Task<int> SaveChangesAsync();
        
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}