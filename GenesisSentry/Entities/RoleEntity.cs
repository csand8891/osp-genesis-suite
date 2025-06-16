using RuleArchitect.Abstractions.Enums; // For ApplicationPermission
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // For Key
using System.ComponentModel.DataAnnotations.Schema; // For Table

namespace GenesisSentry.Entities
{
    [Table("Roles")] // Define table name
    public class RoleEntity
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; }

        // Navigation property for users in this role
        public virtual ICollection<UserEntity> Users { get; set; } = new HashSet<UserEntity>();

        // Store permissions as a collection of enum values. EF Core can handle this.
        public virtual ICollection<ApplicationPermission> Permissions { get; set; } = new HashSet<ApplicationPermission>();
    }
}
