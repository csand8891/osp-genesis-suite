// File: RuleArchitect.Abstractions/DTOs/SoftwareOptionHistoryDto.cs
using System;

namespace RuleArchitect.Abstractions.DTOs
{
    /// <summary>
    /// DTO representing a historical version of a SoftwareOption.
    /// </summary>
    public class SoftwareOptionHistoryDto
    {
        public int SoftwareOptionHistoryId { get; set; }
        public int SoftwareOptionId { get; set; }
        public int Version { get; set; }
        public string? PrimaryName { get; set; }
        public string? AlternativeNames { get; set; }
        public string? SourceFileName { get; set; }
        public string? Notes { get; set; }
        public int? ControlSystemId { get; set; }
        public DateTime ChangeTimestamp { get; set; }
        public string? ChangedBy { get; set; }
    }
}
