using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DocumentCollaboration.Application.DTOs.Auth;
using DocumentCollaboration.Domain.Entities;
using DocumentCollaboration.Domain.Interfaces;

namespace DocumentCollaboration.Application.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<UserDto?> RegisterAsync(RegisterRequest request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task UpdateLastLoginAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            
            _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "DocumentCollaboration";
            _jwtAudience = configuration["Jwt:Audience"] ?? "DocumentCollaborationUsers";
            _jwtExpirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");
            _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            // Find user with role and department
            var user = await _unitOfWork.Users
                .QueryWithIncludes(u => u.Role, u => u.Department!)
                .SingleOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null)
                return null;

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            // Generate tokens
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            // Save refresh token
            await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiry,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            // Update last login
            await UpdateLastLoginAsync(user.UserId);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                User = MapToUserDto(user)
            };
        }

        public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
        {
            var token = await _unitOfWork.RefreshTokens
                .QueryWithIncludes(rt => rt.User, rt => rt.User.Role, rt => rt.User.Department!)
                .SingleOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return null;

            var user = token.User;
            if (!user.IsActive)
                return null;

            // Revoke old token
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(token);

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            // Save new refresh token
            await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.UserId,
                Token = newRefreshToken,
                ExpiresAt = newRefreshTokenExpiry,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                User = MapToUserDto(user)
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _unitOfWork.RefreshTokens
                .SingleOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null)
                return false;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.RefreshTokens.UpdateAsync(token);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<UserDto?> RegisterAsync(RegisterRequest request)
        {
            // Check if username or email already exists
            var existingUser = await _unitOfWork.Users
                .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser)
                return null;

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                RoleId = request.RoleId,
                DepartmentId = request.DepartmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Load role and department
            var createdUser = await _unitOfWork.Users
                .QueryWithIncludes(u => u.Role, u => u.Department!)
                .SingleOrDefaultAsync(u => u.UserId == user.UserId);

            return createdUser != null ? MapToUserDto(createdUser) : null;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            // Hash new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("RoleLevel", user.Role.RoleLevel.ToString()),
                new Claim("DepartmentId", user.DepartmentId?.ToString() ?? ""),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private UserDto MapToUserDto(User user)
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