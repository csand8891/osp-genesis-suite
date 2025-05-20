// In RuleArchitect.ApplicationLogic/DTOs/SoftwareOptionDto.cs (NEW FILE)
using System;

namespace RuleArchitect.ApplicationLogic.DTOs
{
    public class SoftwareOptionDto
    {
        public int SoftwareOptionId { get; set; }
        public string PrimaryName { get; set; }
        public string? AlternativeNames { get; set; }
        public string? SourceFileName { get; set; }
        public string? PrimaryOptionNumberDisplay { get; set; }
        public string? Notes { get; set; }
        public int ControlSystemId { get; set; } // Or int? if it can be null in the DTO
        public string? ControlSystemName { get; set; }
        public int Version { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        // Add any other properties your View might need
    }
}