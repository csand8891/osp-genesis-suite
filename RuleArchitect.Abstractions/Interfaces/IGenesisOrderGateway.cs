using System.IO;
using System.Threading.Tasks;
using RuleArchitect.Abstractions.DTOs.Order; // We'll create this next

namespace GenesisOrderGateway.Interfaces
{
    public interface IGenesisOrderGateway
    {
        /// <summary>
        /// Parses a PDF order from a stream.
        /// </summary>
        /// <param name="pdfStream">The stream containing the PDF data.</param>
        /// <returns>A DTO containing the extracted order information.</returns>
        Task<ParsedOrderDto> ParseOrderPdfAsync(Stream pdfStream);

        /// <summary>
        /// Parses a PDF order from a file path.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        /// <returns>A DTO containing the extracted order information.</returns>
        Task<ParsedOrderDto> ParseOrderPdfAsync(string filePath);
    }
}