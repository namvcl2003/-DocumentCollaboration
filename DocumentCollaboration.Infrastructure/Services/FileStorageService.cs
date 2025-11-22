using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentCollaboration.Infrastructure.Services
{
    /// <summary>
    /// Interface for file storage operations
    /// </summary>
    public interface IFileStorageService
    {
        Task<(bool Success, string? FilePath, string? ErrorMessage)> SaveFileAsync(
            IFormFile file, 
            string subfolder, 
            string? customFileName = null);
        
        Task<bool> DeleteFileAsync(string filePath);
        
        Task<(bool Success, byte[]? FileContent, string? ErrorMessage)> GetFileAsync(string filePath);
        
        Task<bool> FileExistsAsync(string filePath);
        
        string GetPhysicalPath(string relativePath);
        
        Task<(bool Success, string? NewFilePath, string? ErrorMessage)> CopyFileAsync(
            string sourceFilePath, 
            string destinationSubfolder, 
            string? customFileName = null);
    }

    /// <summary>
    /// Service for managing file storage operations
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedExtensions;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _logger = logger;
            
            _basePath = configuration["FileStorage:BasePath"] 
                ?? throw new InvalidOperationException("FileStorage:BasePath not configured");
            
            var maxFileSizeMB = int.Parse(configuration["FileStorage:MaxFileSizeMB"] ?? "50");
            _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
            
            var allowedExtensionsConfig = configuration["FileStorage:AllowedExtensions"] 
                ?? ".docx,.xlsx,.pptx,.doc,.xls,.ppt,.pdf";
            _allowedExtensions = allowedExtensionsConfig.Split(',');

            // Create base directory if it doesn't exist
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created base storage directory: {BasePath}", _basePath);
            }
        }

        public async Task<(bool Success, string? FilePath, string? ErrorMessage)> SaveFileAsync(
            IFormFile file, 
            string subfolder, 
            string? customFileName = null)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return (false, null, "File is empty");
                }

                if (file.Length > _maxFileSizeBytes)
                {
                    return (false, null, $"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)} MB");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!Array.Exists(_allowedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, null, $"File extension {extension} is not allowed");
                }

                // Generate file name
                var fileName = customFileName ?? $"{Guid.NewGuid()}{extension}";
                
                // Create full directory path
                var yearMonth = DateTime.Now.ToString("yyyy/MM");
                var fullDirectory = Path.Combine(_basePath, subfolder, yearMonth);
                
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                // Full file path
                var filePath = Path.Combine(fullDirectory, fileName);
                
                // Save file
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                var relativePath = Path.Combine(subfolder, yearMonth, fileName).Replace("\\", "/");
                
                _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
                
                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                return (false, null, $"Error saving file: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var physicalPath = GetPhysicalPath(filePath);
                
                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                    return false;
                }

                await Task.Run(() => File.Delete(physicalPath));
                
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<(bool Success, byte[]? FileContent, string? ErrorMessage)> GetFileAsync(string filePath)
        {
            try
            {
                var physicalPath = GetPhysicalPath(filePath);
                
                if (!File.Exists(physicalPath))
                {
                    return (false, null, "File not found");
                }

                var fileContent = await File.ReadAllBytesAsync(physicalPath);
                
                return (true, fileContent, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                return (false, null, $"Error reading file: {ex.Message}");
            }
        }

        public Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var physicalPath = GetPhysicalPath(filePath);
                return Task.FromResult(File.Exists(physicalPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public string GetPhysicalPath(string relativePath)
        {
            return Path.Combine(_basePath, relativePath.Replace("/", "\\"));
        }

        public async Task<(bool Success, string? NewFilePath, string? ErrorMessage)> CopyFileAsync(
            string sourceFilePath, 
            string destinationSubfolder, 
            string? customFileName = null)
        {
            try
            {
                var sourcePhysicalPath = GetPhysicalPath(sourceFilePath);
                
                if (!File.Exists(sourcePhysicalPath))
                {
                    return (false, null, "Source file not found");
                }

                var extension = Path.GetExtension(sourceFilePath);
                var fileName = customFileName ?? $"{Guid.NewGuid()}{extension}";
                
                var yearMonth = DateTime.Now.ToString("yyyy/MM");
                var fullDirectory = Path.Combine(_basePath, destinationSubfolder, yearMonth);
                
                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                var destinationPhysicalPath = Path.Combine(fullDirectory, fileName);
                
                await Task.Run(() => File.Copy(sourcePhysicalPath, destinationPhysicalPath, overwrite: false));
                
                var relativePath = Path.Combine(destinationSubfolder, yearMonth, fileName).Replace("\\", "/");
                
                _logger.LogInformation("File copied successfully: {SourcePath} -> {DestinationPath}", 
                    sourceFilePath, relativePath);
                
                return (true, relativePath, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file: {SourcePath}", sourceFilePath);
                return (false, null, $"Error copying file: {ex.Message}");
            }
        }
    }
}