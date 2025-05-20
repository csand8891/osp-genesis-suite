using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuleArchitect.Entities
{
    [Table("OptionNumberRegistries")]
    public class OptionNumberRegistry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OptionNumberRegistryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OptionNumber { get; set; } = null!; // Initialize with null forgiving

        [Required]
        public int SoftwareOptionId { get; set; }

        [ForeignKey("SoftwareOptionId")]
        public virtual SoftwareOption SoftwareOption { get; set; } = null!; // Initialize with null forgiving
    }
}