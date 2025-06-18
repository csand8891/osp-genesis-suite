using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs.Lookups
{
    public class SoftwareOptionLookupDto
    {
        public int SoftwareOptionId { get; set; }
        public string? PrimaryName { get; set; }
        public int? ControlSystemId { get; set; }
    }
}
