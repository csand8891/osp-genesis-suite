using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For Table

namespace GenesisSentry.Entities
{
    [Table("Users")] // Explicitly define table name
    public class UserEntity
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }

        // Email property from existing file, keeping it.
        [Required]
        [MaxLength(256)]
        public string Email { get; set; }


        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; } // From existing file
        public DateTime UpdatedAt { get; set; } // From existing file


        // Change from string Role to a collection of Roles
        public virtual ICollection<RoleEntity> Roles { get; set; } = new HashSet<RoleEntity>();
    }
}
