using System;
using System.Collections.Generic;

namespace DocumentCollaboration.Domain.Entities
{
    /// <summary>
    /// Role entity - defines user roles in the system
    /// RoleLevel: 1=Assistant, 2=ViceManager, 3=Manager, 4=Admin
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int RoleLevel { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}