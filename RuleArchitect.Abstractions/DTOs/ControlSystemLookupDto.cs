using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.Abstractions.DTOs
{
    public class ControlSystemLookupDto
    {
        public int ControlSystemId { get; set; }
        public string Name { get; set; }
        // You might also include MachineTypeName if relevant for display here
        // public string MachineTypeName { get; set; }
    }
}
