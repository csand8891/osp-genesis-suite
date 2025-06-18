using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Entities
{
    [Table("UserActivityLog")]
    public class UserActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserActivityLogId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActivityType { get; set; }

        [MaxLength(100)]
        public string? TargetEntityType { get; set; }

        public int? TargetEntityId { get; set; }

        [MaxLength(255)]
        public string? TargetEntityDescription { get; set; }

        [Required]
        public string Description { get; set; }

        public string? Details { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [ForeignKey("UserId")]
        public virtual GenesisSentry.Entities.UserEntity User { get; set; } = null;

        public UserActivityLog()
        {
            Timestamp = DateTime.UtcNow; // Set default timestamp to current UTC time
        }

    }
}
