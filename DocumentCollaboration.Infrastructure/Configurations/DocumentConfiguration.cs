using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DocumentCollaboration.Domain.Entities;

namespace DocumentCollaboration.Infrastructure.Configurations
{
    /// <summary>
    /// Entity configuration for Document
    /// </summary>
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            // Primary Key
            builder.HasKey(d => d.DocumentId);
            builder.Property(d => d.DocumentId).ValueGeneratedOnAdd();

            // Properties
            builder.Property(d => d.DocumentNumber)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(d => d.Description)
                .HasColumnType("NVARCHAR(MAX)");

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.FileExtension)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(d => d.FilePath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(d => d.CollaboraFileId)
                .HasMaxLength(255);

            builder.Property(d => d.CurrentWorkflowLevel)
                .HasDefaultValue(1);

            builder.Property(d => d.Priority)
                .HasDefaultValue(2);

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            builder.Property(d => d.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Indexes
            builder.HasIndex(d => d.DocumentNumber).IsUnique();
            builder.HasIndex(d => d.StatusId);
            builder.HasIndex(d => d.CreatedByUserId);
            builder.HasIndex(d => d.CurrentHandlerUserId);
            builder.HasIndex(d => d.DepartmentId);
            builder.HasIndex(d => d.CategoryId);
            builder.HasIndex(d => d.CreatedAt);
            builder.HasIndex(d => d.CurrentWorkflowLevel);

            // Relationships are configured in ApplicationDbContext
        }
    }
}