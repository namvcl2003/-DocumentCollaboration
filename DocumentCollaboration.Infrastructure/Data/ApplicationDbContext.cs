using Microsoft.EntityFrameworkCore;
using DocumentCollaboration.Domain.Entities;

namespace DocumentCollaboration.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentCategory> DocumentCategories { get; set; } = null!;
        public DbSet<DocumentStatus> DocumentStatuses { get; set; } = null!;
        public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;
        public DbSet<WorkflowAction> WorkflowActions { get; set; } = null!;
        public DbSet<DocumentWorkflowHistory> DocumentWorkflowHistory { get; set; } = null!;
        public DbSet<DocumentAssignment> DocumentAssignments { get; set; } = null!;
        public DbSet<CollaboraSession> CollaboraSessions { get; set; } = null!;
        public DbSet<DocumentComment> DocumentComments { get; set; } = null!;
        public DbSet<NotificationType> NotificationTypes { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations from separate files
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // // Configure table names to match existing database
            // modelBuilder.Entity<User>().ToTable("Users");
            // modelBuilder.Entity<Role>().ToTable("Roles");
            // modelBuilder.Entity<Department>().ToTable("Departments");
            // modelBuilder.Entity<Document>().ToTable("Documents");
            // modelBuilder.Entity<DocumentCategory>().ToTable("DocumentCategories");
            // modelBuilder.Entity<DocumentStatus>().ToTable("DocumentStatuses");
            // modelBuilder.Entity<DocumentVersion>().ToTable("DocumentVersions");
            // modelBuilder.Entity<WorkflowAction>().ToTable("WorkflowActions");
            // modelBuilder.Entity<DocumentWorkflowHistory>().ToTable("DocumentWorkflowHistory");
            // modelBuilder.Entity<DocumentAssignment>().ToTable("DocumentAssignments");
            // modelBuilder.Entity<CollaboraSession>().ToTable("CollaboraSessions");
            // modelBuilder.Entity<DocumentComment>().ToTable("DocumentComments");
            // modelBuilder.Entity<NotificationType>().ToTable("NotificationTypes");
            // modelBuilder.Entity<Notification>().ToTable("Notifications");
            // modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");
            // modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
            // modelBuilder.Entity<SystemSetting>().ToTable("SystemSettings");

            // Configure relationships and constraints
            ConfigureUserRelationships(modelBuilder);
            ConfigureDocumentRelationships(modelBuilder);
            ConfigureWorkflowRelationships(modelBuilder);
            ConfigureOtherRelationships(modelBuilder);
        }

        private void ConfigureUserRelationships(ModelBuilder modelBuilder)
        {
            // User - Role relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // User - Department relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Department - Manager/ViceManager relationships
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.ViceManager)
                .WithMany()
                .HasForeignKey(d => d.ViceManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureDocumentRelationships(ModelBuilder modelBuilder)
        {
            // Document - Category
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Document - Status
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Status)
                .WithMany(s => s.Documents)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Document - CreatedByUser
            modelBuilder.Entity<Document>()
                .HasOne(d => d.CreatedByUser)
                .WithMany(u => u.CreatedDocuments)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Document - CurrentHandlerUser
            modelBuilder.Entity<Document>()
                .HasOne(d => d.CurrentHandlerUser)
                .WithMany(u => u.HandlingDocuments)
                .HasForeignKey(d => d.CurrentHandlerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Document - Department
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Department)
                .WithMany(dept => dept.Documents)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentVersion - Document
            modelBuilder.Entity<DocumentVersion>()
                .HasOne(dv => dv.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(dv => dv.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentVersion - CreatedByUser
            modelBuilder.Entity<DocumentVersion>()
                .HasOne(dv => dv.CreatedByUser)
                .WithMany(u => u.DocumentVersions)
                .HasForeignKey(dv => dv.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint for DocumentNumber
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.DocumentNumber)
                .IsUnique();
        }

        private void ConfigureWorkflowRelationships(ModelBuilder modelBuilder)
        {
            // DocumentWorkflowHistory - Document
            modelBuilder.Entity<DocumentWorkflowHistory>()
                .HasOne(h => h.Document)
                .WithMany(d => d.WorkflowHistory)
                .HasForeignKey(h => h.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentWorkflowHistory - Version
            modelBuilder.Entity<DocumentWorkflowHistory>()
                .HasOne(h => h.Version)
                .WithMany(v => v.WorkflowHistory)
                .HasForeignKey(h => h.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentWorkflowHistory - Action
            modelBuilder.Entity<DocumentWorkflowHistory>()
                .HasOne(h => h.Action)
                .WithMany(a => a.WorkflowHistory)
                .HasForeignKey(h => h.ActionId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentWorkflowHistory - FromUser
            modelBuilder.Entity<DocumentWorkflowHistory>()
                .HasOne(h => h.FromUser)
                .WithMany()
                .HasForeignKey(h => h.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentWorkflowHistory - ToUser
            modelBuilder.Entity<DocumentWorkflowHistory>()
                .HasOne(h => h.ToUser)
                .WithMany()
                .HasForeignKey(h => h.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentAssignment relationships
            modelBuilder.Entity<DocumentAssignment>()
                .HasOne(a => a.Document)
                .WithMany(d => d.Assignments)
                .HasForeignKey(a => a.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentAssignment>()
                .HasOne(a => a.AssignedToUser)
                .WithMany(u => u.AssignedDocuments)
                .HasForeignKey(a => a.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentAssignment>()
                .HasOne(a => a.AssignedByUser)
                .WithMany(u => u.CreatedAssignments)
                .HasForeignKey(a => a.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureOtherRelationships(ModelBuilder modelBuilder)
        {
            // CollaboraSession relationships
            modelBuilder.Entity<CollaboraSession>()
                .HasOne(cs => cs.Document)
                .WithMany(d => d.CollaboraSessions)
                .HasForeignKey(cs => cs.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CollaboraSession>()
                .HasOne(cs => cs.Version)
                .WithMany(v => v.CollaboraSessions)
                .HasForeignKey(cs => cs.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CollaboraSession>()
                .HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentComment relationships
            modelBuilder.Entity<DocumentComment>()
                .HasOne(c => c.Document)
                .WithMany(d => d.Comments)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentComment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.NotificationType)
                .WithMany(nt => nt.Notifications)
                .HasForeignKey(n => n.NotificationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Document)
                .WithMany(d => d.Notifications)
                .HasForeignKey(n => n.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // RefreshToken relationship
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(ss => ss.SettingKey)
                .IsUnique();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-update timestamps
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is User user)
                    {
                        user.CreatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is Document doc)
                    {
                        doc.CreatedAt = DateTime.UtcNow;
                        doc.UpdatedAt = DateTime.UtcNow;
                    }
                    // Add more entities as needed
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is User user)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is Document doc)
                    {
                        doc.UpdatedAt = DateTime.UtcNow;
                    }
                    // Add more entities as needed
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}