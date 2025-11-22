using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using DocumentCollaboration.Domain.Entities;
using DocumentCollaboration.Domain.Interfaces;
using DocumentCollaboration.Infrastructure.Data;

namespace DocumentCollaboration.Infrastructure.Repositories
{
    /// <summary>
    /// Unit of Work implementation for transaction management
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy-loaded repositories
        private IRepository<User>? _users;
        private IRepository<Role>? _roles;
        private IRepository<Department>? _departments;
        private IRepository<Document>? _documents;
        private IRepository<DocumentCategory>? _documentCategories;
        private IRepository<DocumentStatus>? _documentStatuses;
        private IRepository<DocumentVersion>? _documentVersions;
        private IRepository<WorkflowAction>? _workflowActions;
        private IRepository<DocumentWorkflowHistory>? _documentWorkflowHistory;
        private IRepository<DocumentAssignment>? _documentAssignments;
        private IRepository<CollaboraSession>? _collaboraSessions;
        private IRepository<DocumentComment>? _documentComments;
        private IRepository<NotificationType>? _notificationTypes;
        private IRepository<Notification>? _notifications;
        private IRepository<AuditLog>? _auditLogs;
        private IRepository<RefreshToken>? _refreshTokens;
        private IRepository<SystemSetting>? _systemSettings;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // Repository properties with lazy initialization
        public IRepository<User> Users => 
            _users ??= new Repository<User>(_context);

        public IRepository<Role> Roles => 
            _roles ??= new Repository<Role>(_context);

        public IRepository<Department> Departments => 
            _departments ??= new Repository<Department>(_context);

        public IRepository<Document> Documents => 
            _documents ??= new Repository<Document>(_context);

        public IRepository<DocumentCategory> DocumentCategories => 
            _documentCategories ??= new Repository<DocumentCategory>(_context);

        public IRepository<DocumentStatus> DocumentStatuses => 
            _documentStatuses ??= new Repository<DocumentStatus>(_context);

        public IRepository<DocumentVersion> DocumentVersions => 
            _documentVersions ??= new Repository<DocumentVersion>(_context);

        public IRepository<WorkflowAction> WorkflowActions => 
            _workflowActions ??= new Repository<WorkflowAction>(_context);

        public IRepository<DocumentWorkflowHistory> DocumentWorkflowHistory => 
            _documentWorkflowHistory ??= new Repository<DocumentWorkflowHistory>(_context);

        public IRepository<DocumentAssignment> DocumentAssignments => 
            _documentAssignments ??= new Repository<DocumentAssignment>(_context);

        public IRepository<CollaboraSession> CollaboraSessions => 
            _collaboraSessions ??= new Repository<CollaboraSession>(_context);

        public IRepository<DocumentComment> DocumentComments => 
            _documentComments ??= new Repository<DocumentComment>(_context);

        public IRepository<NotificationType> NotificationTypes => 
            _notificationTypes ??= new Repository<NotificationType>(_context);

        public IRepository<Notification> Notifications => 
            _notifications ??= new Repository<Notification>(_context);

        public IRepository<AuditLog> AuditLogs => 
            _auditLogs ??= new Repository<AuditLog>(_context);

        public IRepository<RefreshToken> RefreshTokens => 
            _refreshTokens ??= new Repository<RefreshToken>(_context);

        public IRepository<SystemSetting> SystemSettings => 
            _systemSettings ??= new Repository<SystemSetting>(_context);

        // Transaction management
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}