using System.Collections.Generic;
using System.Linq; // Required for .Any()

namespace RuleArchitect.Abstractions.DTOs.Order
{
    /// <summary>
    /// Data Transfer Object (DTO) that holds the information extracted 
    /// from a parsed order document (e.g., a PDF).
    /// </summary>
    public class ParsedOrderDto
    {
        /// <summary>
        /// Gets or sets the extracted Sales Order Number.
        /// Can be null if not found during parsing.
        /// </summary>
        public string SalesOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the extracted Machine Serial Number.
        /// Can be null if not found during parsing.
        /// </summary>
        public string MachineSerialNumber { get; set; }

        public string ControlSystemName { get; set; }

        /// <summary>
        /// Gets or sets a list of line items parsed from the order document.
        /// Each item represents a <see cref="ParsedLineItemDto"/>.
        /// </summary>
        public List<ParsedLineItemDto> LineItems { get; set; } = new List<ParsedLineItemDto>();

        /// <summary>
        /// Gets or sets a list of any errors encountered during the parsing process.
        /// </summary>
        public List<string> ParsingErrors { get; set; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether the parsing was considered successful
        /// (i.e., no parsing errors were recorded).
        /// </summary>
        public bool IsSuccess => !ParsingErrors.Any();
    }

    /// <summary>
    /// Data Transfer Object (DTO) that represents a single line item
    /// extracted from a parsed order document.
    /// </summary>
    public class ParsedLineItemDto
    {
        /// <summary>
        /// Gets or sets the raw text of the line item as it appeared in the document.
        /// This can be useful for debugging or display purposes.
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// Gets or sets the extracted Software Option Number (or part number) for this line item.
        /// This is typically the identifier that links to a <see cref="RuleArchitect.Entities.SoftwareOption"/>.
        /// </summary>
        public string SoftwareOptionNumber { get; set; }

        /// <summary>
        /// Gets or sets the quantity for this line item. Defaults to 1.
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Gets or sets the extracted description for this line item.
        /// </summary>
        public string Description { get; set; }
    }
}
