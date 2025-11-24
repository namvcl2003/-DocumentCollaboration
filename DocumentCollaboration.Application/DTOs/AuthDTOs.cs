// using System;
//
// namespace DocumentCollaboration.Application.DTOs.Auth
// {
//     // ===== REQUEST DTOs =====
//     
//     public class LoginRequest
//     {
//         public string Username { get; set; } = string.Empty;
//         public string Password { get; set; } = string.Empty;
//     }
//
//     public class RegisterRequest
//     {
//         public string Username { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string Password { get; set; } = string.Empty;
//         public string FullName { get; set; } = string.Empty;
//         public int RoleId { get; set; }
//         public int? DepartmentId { get; set; }
//     }
//
//     public class RefreshTokenRequest
//     {
//         public string RefreshToken { get; set; } = string.Empty;
//     }
//
//     public class ChangePasswordRequest
//     {
//         public string CurrentPassword { get; set; } = string.Empty;
//         public string NewPassword { get; set; } = string.Empty;
//     }
//
//     // ===== RESPONSE DTOs =====
//     
//     public class LoginResponse
//     {
//         public string AccessToken { get; set; } = string.Empty;
//         public string RefreshToken { get; set; } = string.Empty;
//         public DateTime ExpiresAt { get; set; }
//         public UserDto User { get; set; } = null!;
//     }
//
//     public class UserDto
//     {
//         public int UserId { get; set; }
//         public string Username { get; set; } = string.Empty;
//         public string Email { get; set; } = string.Empty;
//         public string FullName { get; set; } = string.Empty;
//         public int RoleId { get; set; }
//         public string RoleName { get; set; } = string.Empty;
//         public int RoleLevel { get; set; }
//         public int? DepartmentId { get; set; }
//         public string? DepartmentName { get; set; }
//         public bool IsActive { get; set; }
//         public DateTime? LastLoginAt { get; set; }
//         public DateTime CreatedAt { get; set; }
//     }
//
//     public class RoleDto
//     {
//         public int RoleId { get; set; }
//         public string RoleName { get; set; } = string.Empty;
//         public int RoleLevel { get; set; }
//         public string? Description { get; set; }
//     }
//     public class UpdateUserRequest
//     {
//         public string Email { get; set; } = string.Empty;
//         public string FullName { get; set; } = string.Empty;
//         public int RoleId { get; set; }
//         public int? DepartmentId { get; set; }
//     }
//
//     public class DepartmentDto
//     {
//         public int DepartmentId { get; set; }
//         public string DepartmentName { get; set; } = string.Empty;
//         public string DepartmentCode { get; set; } = string.Empty;
//         public string? Description { get; set; }
//         public string? ManagerName { get; set; }
//         public string? ViceManagerName { get; set; }
//         public bool IsActive { get; set; }
//     }
// }


using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Application.DTOs.Auth
{
    // Authentication DTOs
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    // User DTOs
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int RoleLevel { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }
    }

    // Role DTOs
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public int RoleLevel { get; set; }
        public string? Description { get; set; }
    }

    // Department DTOs
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ManagerName { get; set; }
        public string? ViceManagerName { get; set; }
        public bool IsActive { get; set; }
    }

    // Notification DTOs
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? DocumentId { get; set; }
        public int? CommentId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationQueryRequest
    {
        public bool? IsRead { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class UnreadNotificationCountResponse
    {
        public int Count { get; set; }
    }

    // Paged Result (if not already in Documents namespace)
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}