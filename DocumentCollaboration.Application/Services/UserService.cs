using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DocumentCollaboration.Application.DTOs.Auth;
// using DocumentCollaboration.API.Controllers;
using DocumentCollaboration.Domain.Interfaces;
using DocumentCollaboration.Application.DTOs.Auth;

namespace DocumentCollaboration.Application.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<List<UserDto>> GetUsersByDepartmentAsync(int departmentId);
        Task<List<UserDto>> GetUsersByRoleLevelAsync(int roleLevel);
        Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<bool> DeactivateUserAsync(int userId);
        Task<List<RoleDto>> GetRolesAsync();
        Task<List<DepartmentDto>> GetDepartmentsAsync();
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users
                    .QueryWithIncludes(u => u.Role, u => u.Department!)
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users
                    .QueryWithIncludes(u => u.Role, u => u.Department!)
                    .SingleOrDefaultAsync(u => u.UserId == userId);

                return user != null ? MapToUserDto(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<UserDto>> GetUsersByDepartmentAsync(int departmentId)
        {
            try
            {
                var users = await _unitOfWork.Users
                    .QueryWithIncludes(u => u.Role, u => u.Department!)
                    .Where(u => u.DepartmentId == departmentId && u.IsActive)
                    .OrderBy(u => u.Role.RoleLevel)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();

                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by department {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<List<UserDto>> GetUsersByRoleLevelAsync(int roleLevel)
        {
            try
            {
                var users = await _unitOfWork.Users
                    .QueryWithIncludes(u => u.Role, u => u.Department!)
                    .Where(u => u.Role.RoleLevel == roleLevel && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                return users.Select(MapToUserDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role level {RoleLevel}", roleLevel);
                throw;
            }
        }

        public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return null;

                user.Email = request.Email;
                user.FullName = request.FullName;
                user.RoleId = request.RoleId;
                user.DepartmentId = request.DepartmentId;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return await GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            try
            {
                var roles = await _unitOfWork.Roles
                    .GetAllAsync();

                return roles.Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    RoleLevel = r.RoleLevel,
                    Description = r.Description
                }).OrderBy(r => r.RoleLevel).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                throw;
            }
        }

        public async Task<List<DepartmentDto>> GetDepartmentsAsync()
        {
            try
            {
                var departments = await _unitOfWork.Departments
                    .QueryWithIncludes(d => d.Manager!, d => d.ViceManager!)
                    .Where(d => d.IsActive)
                    .ToListAsync();

                return departments.Select(d => new DepartmentDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    ManagerName = d.Manager?.FullName,
                    ViceManagerName = d.ViceManager?.FullName,
                    IsActive = d.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments");
                throw;
            }
        }

        private UserDto MapToUserDto(Domain.Entities.User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleId = user.RoleId,
                RoleName = user.Role.RoleName,
                RoleLevel = user.Role.RoleLevel,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department?.DepartmentName,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };
        }
    }
}