using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs
{
    public class SpecCodeDefinitionDetailDto
    {
        public int SpecCodeDefinitionId { get; set; }
        public int ControlSystemId { get; set; } // To link back or filter by ControlSystem
        public string ControlSystemName { get; set; } // For display
        public string Category { get; set; }
        public string SpecCodeNo { get; set; }
        public string SpecCodeBit { get; set; }
        public string Description { get; set; }
    }
}
