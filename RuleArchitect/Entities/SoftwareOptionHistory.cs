using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("SoftwareOptionHistories")]
    public class SoftwareOptionHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SoftwareOptionHistoryId { get; set; }

        [Required]
        public int SoftwareOptionId { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        [MaxLength(255)]
        public string PrimaryName { get; set; } = null!;

        [MaxLength(500)]
        public string? AlternativeNames { get; set; }

        [MaxLength(255)]
        public string? SourceFileName { get; set; }

        [MaxLength(100)]
        public string? PrimaryOptionNumberDisplay { get; set; }

        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? CheckedBy { get; set; }

        public DateTime? CheckedDate { get; set; }

        public int? ControlSystemId { get; set; }

        [Required]
        public DateTime ChangeTimestamp { get; set; }

        [MaxLength(100)]
        public string? ChangedBy { get; set; }

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption SoftwareOption { get; set; } = null!;
    }
}