using GenesisOrderGateway.DTOs;
using GenesisOrderGateway.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text; // For StringBuilder
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Collections.Generic; // For List

namespace GenesisOrderGateway.Services
{
    /// <summary>
    /// Service to parse PDF order documents and extract relevant information.
    /// </summary>
    public class PdfOrderGatewayService : IGenesisOrderGateway
    {
        // Pre-compile Regex for performance
        private static readonly Regex SalesOrderRegex = new Regex(@"Sales Order\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SerialNumberRegex = new Regex(@"Serial\s*#\s*:\s*([\w\.\-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ControlSystemRegex = new Regex(@"Control\s*Type\s*:\s*([\w\-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ItemStartRegex = new Regex(@"^([A-Z0-9]+[\.\-][A-Z0-9\.\-]+)\s+(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly List<string> KnownSeparatorPhrases = new List<string> {
            "SOME OTHER TEXT IN BETWEEN",
            "Header Text - Should be ignored",
            "Footer Text - Should be ignored",
            "SUBTOTAL", // Example: Add more known separators
            "TOTAL",
            "TAX",
            "SHIPPING"
        }.Select(s => s.Trim().ToUpperInvariant()).ToList(); // Store them trimmed and uppercase for consistent comparison

        /// <summary>
        /// Parses a PDF order from a file path.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        /// <returns>A DTO containing the extracted order information.</returns>
        public async Task<ParsedOrderDto> ParseOrderPdfAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new ParsedOrderDto { ParsingErrors = { "File not found at path: " + filePath } };
            }

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return await ParseOrderPdfAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                return new ParsedOrderDto { ParsingErrors = { $"Error opening file {filePath}: {ex.Message}" } };
            }
        }

        /// <summary>
        /// Parses a PDF order from a stream.
        /// </summary>
        /// <param name="pdfStream">The stream containing the PDF data.</param>
        /// <returns>A DTO containing the extracted order information.</returns>
        public async Task<ParsedOrderDto> ParseOrderPdfAsync(Stream pdfStream)
        {
            var result = new ParsedOrderDto();
            var allPageTextBuilder = new StringBuilder();

            try
            {
                using (PdfDocument document = PdfDocument.Open(pdfStream))
                {
                    foreach (Page page in document.GetPages())
                    {
                        string pageText = page.Text;
                        allPageTextBuilder.AppendLine(pageText);

                        if (string.IsNullOrEmpty(result.SalesOrderNumber))
                            FindSalesOrderNumber(pageText, result);

                        if (string.IsNullOrEmpty(result.MachineSerialNumber))
                            FindMachineSerialNumber(pageText, result);

                        if (string.IsNullOrEmpty(result.ControlSystemName))
                            FindControlSystem(pageText, result);
                    }
                }

                FindLineItems(allPageTextBuilder.ToString(), result);

            }
            catch (Exception ex)
            {
                result.ParsingErrors.Add($"PDF processing error: {ex.Message}");
            }

            ValidateResult(result);
            return await Task.FromResult(result);
        }

        internal void FindSalesOrderNumber(string text, ParsedOrderDto result)
        {
            if (!string.IsNullOrEmpty(result.SalesOrderNumber)) return;
            var match = SalesOrderRegex.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                result.SalesOrderNumber = match.Groups[1].Value.Trim();
            }
        }

        internal void FindMachineSerialNumber(string text, ParsedOrderDto result)
        {
            if (!string.IsNullOrEmpty(result.MachineSerialNumber)) return;
            var match = SerialNumberRegex.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                result.MachineSerialNumber = match.Groups[1].Value.Trim();
            }
        }

        internal void FindControlSystem(string text, ParsedOrderDto result)
        {
            if (!string.IsNullOrEmpty(result.ControlSystemName)) return;
            var match = ControlSystemRegex.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                result.ControlSystemName = match.Groups[1].Value.Trim();
            }
        }

        internal void FindLineItems(string fullText, ParsedOrderDto result)
        {
            var lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            ParsedLineItemDto currentItem = null;
            var descriptionBuilder = new StringBuilder();

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                Match itemMatch = ItemStartRegex.Match(trimmedLine);

                // Check if the line starts with any of the known separator phrases (case-insensitive)
                string upperTrimmedLine = trimmedLine.ToUpperInvariant();
                bool isKnownSeparatorLine = KnownSeparatorPhrases.Any(phrase => upperTrimmedLine.StartsWith(phrase));

                if (itemMatch.Success && itemMatch.Groups.Count > 2)
                {
                    // Found a new item. Finalize the previous one if it exists.
                    if (currentItem != null)
                    {
                        currentItem.Description = descriptionBuilder.ToString().Trim();
                        result.LineItems.Add(currentItem);
                    }

                    // Start the new item
                    descriptionBuilder.Clear();
                    currentItem = new ParsedLineItemDto
                    {
                        SoftwareOptionNumber = itemMatch.Groups[1].Value.Trim(),
                        RawText = trimmedLine
                    };
                    descriptionBuilder.Append(itemMatch.Groups[2].Value.Trim());
                }
                else if (isKnownSeparatorLine && currentItem != null)
                {
                    // This line is a known separator. Finalize the current item.
                    currentItem.Description = descriptionBuilder.ToString().Trim();
                    result.LineItems.Add(currentItem);
                    currentItem = null; // Reset current item, this separator line is not part of any item.
                    descriptionBuilder.Clear();
                }
                else if (currentItem != null) // Not an item start, not a known separator, but an item is active
                {
                    // This line is a continuation of the current item's description.
                    if (descriptionBuilder.Length > 0)
                    {
                        descriptionBuilder.Append(" ");
                    }
                    descriptionBuilder.Append(trimmedLine);
                    currentItem.RawText += Environment.NewLine + trimmedLine;
                }
            }

            // Add the very last item being processed after the loop finishes.
            if (currentItem != null)
            {
                currentItem.Description = descriptionBuilder.ToString().Trim();
                result.LineItems.Add(currentItem);
            }
        }

        internal void ValidateResult(ParsedOrderDto result)
        {
            if (string.IsNullOrEmpty(result.SalesOrderNumber))
            {
                result.ParsingErrors.Add("Sales Order Number not found.");
            }
            if (string.IsNullOrEmpty(result.MachineSerialNumber))
            {
                result.ParsingErrors.Add("Machine Serial Number not found.");
            }
            if (string.IsNullOrEmpty(result.ControlSystemName))
            {
                result.ParsingErrors.Add("Control System Name not found.");
            }
            if (!result.LineItems.Any() &&
                result.ParsingErrors.All(e => !e.Contains("No line items could be identified.") && !e.Contains("No line items found or parsed.")))
            {
                // result.ParsingErrors.Add("No line items found or parsed."); 
            }
        }
    }
}
