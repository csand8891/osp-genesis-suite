using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs.Lookups
{
    public class SpecCodeDefinitionLookupDto
    {
        public int SpecCodeDefinitionId { get; set; }
        public string? DisplayName { get; set; }
        public int ControlSystemId { get; set; }
    }
}
