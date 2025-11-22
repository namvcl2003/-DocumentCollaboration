using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DocumentCollaboration.Domain.Interfaces;

namespace DocumentCollaboration.Infrastructure.Services
{
    /// <summary>
    /// Interface for generating document numbers
    /// </summary>
    public interface IDocumentNumberGenerator
    {
        Task<string> GenerateDocumentNumberAsync(int? departmentId = null);
    }

    /// <summary>
    /// Service for generating unique document numbers
    /// Format: DOC-YYYYMMDD-XXXX
    /// </summary>
    public class DocumentNumberGenerator : IDocumentNumberGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _prefix;

        public DocumentNumberGenerator(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _prefix = configuration["FileStorage:DocumentNumberPrefix"] ?? "DOC";
        }

        public async Task<string> GenerateDocumentNumberAsync(int? departmentId = null)
        {
            var today = DateTime.Now;
            var dateString = today.ToString("yyyyMMdd");
            
            // Get department code if applicable
            string departmentCode = "";
            if (departmentId.HasValue)
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(departmentId.Value);
                if (department != null && !string.IsNullOrEmpty(department.DepartmentCode))
                {
                    departmentCode = $"-{department.DepartmentCode}";
                }
            }

            // Find the latest document number for today
            var todayStart = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
            var todayEnd = todayStart.AddDays(1);

            var latestDocument = await _unitOfWork.Documents
                .Query()
                .Where(d => d.CreatedAt >= todayStart && d.CreatedAt < todayEnd)
                .OrderByDescending(d => d.DocumentNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            
            if (latestDocument != null && !string.IsNullOrEmpty(latestDocument.DocumentNumber))
            {
                // Extract sequence number from last document
                var parts = latestDocument.DocumentNumber.Split('-');
                if (parts.Length >= 3 && int.TryParse(parts[^1], out int lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            // Format: DOC-YYYYMMDD-XXXX or DOC-DEPT_CODE-YYYYMMDD-XXXX
            var documentNumber = $"{_prefix}{departmentCode}-{dateString}-{sequence:D4}";
            
            return documentNumber;
        }
    }
}