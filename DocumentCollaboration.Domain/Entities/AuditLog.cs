// namespace DocumentCollaboration.Domain.Entities;
//
// /// <summary>
// /// Lưu lịch sử thay đổi của tài liệu
// /// </summary>
// public class AuditLog
// {
//     public int Id { get; set; } // Primary Key
//     public int DocumentId { get; set; }
//     public int UserId { get; set; }
//     public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "Approved", "Rejected"
//     public string? Details { get; set; } // JSON string chứa chi tiết thay đổi
//     public DateTime Timestamp { get; set; }
//     
//     // Navigation Properties
//     public Document? Document { get; set; }
//     public User? User { get; set; }
// }