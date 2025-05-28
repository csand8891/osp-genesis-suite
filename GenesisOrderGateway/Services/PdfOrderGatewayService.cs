using GenesisOrderGateway.DTOs;
using GenesisOrderGateway.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Collections.Generic;

namespace GenesisOrderGateway.Services
{
    public class PdfOrderGatewayService : IGenesisOrderGateway
    {
        private static readonly Regex SalesOrderRegex = new Regex(@"Sales Order\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SerialNumberRegex = new Regex(@"Serial\s*#\s*:\s*([\w\.\-]+?)(?=\s*Model|:|\s|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ControlSystemRegex = new Regex(@"Control\s*Type\s*:\s*([\w\-]+?)(?=\s*Warranty|:|\s\$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // In PdfOrderGatewayService.cs

        // Core patterns for item codes (these remain the same)
        private const string CoreFormat1Pattern = @"[A-Z0-9]{8}\.[MP]\d{4}-C\d{2}";
        private const string CoreFormat2Pattern = @"[A-Z]{2,4}-\d{3,6}(?:-[A-Z0-9]{3,7})*-\d{1,3}";

        // Simplified and corrected construction for the combined pattern string
        private static readonly string FinalCombinedPatternString =
            @"(?<itemcode>" + // Start named group 'itemcode'
            CoreFormat1Pattern +
            @"|" +              // OR operator
            CoreFormat2Pattern +
            @")";               // End named group 'itemcode'

        private static readonly Regex CombinedItemCodeRegex = new Regex(
            FinalCombinedPatternString, // Use the separately constructed string
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly List<string> KnownSeparatorPhrases = new List<string> {
            "SUBTOTAL", "TOTAL", "TAX", "SHIPPING", "ITEM NUMBER", "DESCRIPTION", "SHIP DATE", "QUANTITY",
            "UNIT", "SALES PRICE", "DISCOUNT", "DISCOUNT PERCENT", "AMOUNT",
            "ITEM NUMBERDESCRIPTIONSHIP DATEQUANTITYUNITSALES PRICEDISCOUNTDISCOUNT PERCENTAMOUNT",
            "CURRENCYTOTALUSD", "SALES ORDER", "SERIAL #", "MODEL #", "CONTROL TYPE", "WARRANTY", "CUSTOMER",
            "PROJECT", "ORDER DATE", "COMMENTS", "FORWARDING AGENT", "CONTRACT #", "PAGE",
            "SOME OTHER TEXT IN BETWEEN", "HEADER TEXT - SHOULD BE IGNORED", "FOOTER TEXT - SHOULD BE IGNORED"
        }.Select(s => s.Trim().ToUpperInvariant()).ToList();

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
                        string pageText = page.Text ?? string.Empty;
                        allPageTextBuilder.AppendLine(pageText);
                        if (string.IsNullOrEmpty(result.SalesOrderNumber)) FindSalesOrderNumber(pageText, result);
                        if (string.IsNullOrEmpty(result.MachineSerialNumber)) FindMachineSerialNumber(pageText, result);
                        if (string.IsNullOrEmpty(result.ControlSystemName)) FindControlSystem(pageText, result);
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
                result.SalesOrderNumber = match.Groups[1].Value.Trim();
        }

        internal void FindMachineSerialNumber(string text, ParsedOrderDto result)
        {
            if (!string.IsNullOrEmpty(result.MachineSerialNumber)) return;
            var match = SerialNumberRegex.Match(text);
            if (match.Success && match.Groups.Count > 1)
                result.MachineSerialNumber = match.Groups[1].Value.Trim();
        }

        internal void FindControlSystem(string text, ParsedOrderDto result)
        {
            if (!string.IsNullOrEmpty(result.ControlSystemName)) return;
            var match = ControlSystemRegex.Match(text);
            if (match.Success && match.Groups.Count > 1)
                result.ControlSystemName = match.Groups[1].Value.Trim();
        }

        internal void FindLineItems(string fullText, ParsedOrderDto result)
        {
            MatchCollection itemMatches = CombinedItemCodeRegex.Matches(fullText);
            if (itemMatches.Count == 0)
            {
                // Optional: Add a parsing error if verbosity is desired
                // result.ParsingErrors.Add("No items found matching defined patterns.");
                return;
            }

            for (int i = 0; i < itemMatches.Count; i++)
            {
                Match currentMatch = itemMatches[i];
                string itemNumber = currentMatch.Groups["itemcode"].Value;

                int blockStart = currentMatch.Index + currentMatch.Length;
                int blockEnd = (i + 1 < itemMatches.Count) ? itemMatches[i + 1].Index : fullText.Length;
                string itemBlockText = fullText.Substring(blockStart, blockEnd - blockStart).TrimStart();

                string description = itemBlockText;

                var dataFieldTerminators = new Regex[] {
                    new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b"),      // Date
                    new Regex(@"\b\d+\.\d{2}EA\b"),                // Quantity like 1.00EA
                    new Regex(@"\b\d{1,3}(?:,\d{3})*\.\d{2}\b"),    // Price
                    new Regex(@"\b(?:USD|EUR|GBP)\b", RegexOptions.IgnoreCase) // Currency
                };

                int earliestTerminatorIndex = itemBlockText.Length;
                foreach (var terminatorRegex in dataFieldTerminators)
                {
                    Match termMatch = terminatorRegex.Match(itemBlockText);
                    if (termMatch.Success && termMatch.Index < earliestTerminatorIndex)
                    {
                        earliestTerminatorIndex = termMatch.Index;
                    }
                }

                description = itemBlockText.Substring(0, earliestTerminatorIndex).Trim();
                description = description.TrimEnd(':', ',', ';', '.', '-'); // Clean common trailing punctuation
                description = Regex.Replace(description, @"\s+$", ""); // remove any trailing spaces again


                if (IsLikelyHeaderOrJunk(itemNumber, description))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(description) && description.Length >= 3)
                {
                    result.LineItems.Add(new ParsedLineItemDto
                    {
                        SoftwareOptionNumber = itemNumber,
                        Description = description,
                        RawText = currentMatch.Value + " " + itemBlockText.Substring(0, Math.Min(itemBlockText.Length, description.Length + 40))
                    });
                }
            }
        }

        // Simplified IsLikelyHeaderOrJunk, relying more on the specificity of CombinedItemCodeRegex
        private bool IsLikelyHeaderOrJunk(string itemNumber, string description)
        {
            if (string.IsNullOrWhiteSpace(itemNumber)) return true;

            string itemUpper = itemNumber.ToUpperInvariant();
            string descUpper = (description ?? string.Empty).ToUpperInvariant().Trim();

            // If item number itself is a known junk phrase
            if (KnownSeparatorPhrases.Contains(itemUpper)) return true;

            // If description is a known junk phrase and item is not very distinct
            if (KnownSeparatorPhrases.Contains(descUpper) && descUpper.Length > 5)
            { // Avoid matching short descriptions like "UNIT" if item is valid
                if (itemUpper.Length < 6 && !itemUpper.Any(char.IsDigit)) return true; // Item "PART" desc "UNIT"
            }

            // If the entire matched block (item + start of desc) starts with a known junk phrase
            string combinedStart = (itemUpper + " " + descUpper).TrimStart();
            if (KnownSeparatorPhrases.Any(phrase => combinedStart.StartsWith(phrase) && phrase.Length > itemUpper.Length && phrase.Length > 5)) return true;


            // Very short, non-alphabetic descriptions are suspect if item is also generic
            if (descUpper.Length < 3 && !descUpper.Any(char.IsLetter))
            {
                if (itemUpper.Length < 5 && !itemUpper.Any(char.IsLetter)) return true;
            }

            // Catch cases where item number is a price and description is minimal
            if (Regex.IsMatch(itemUpper, @"^\d+([\.\-]\d+)+$") && itemUpper.All(c => char.IsDigit(c) || c == '.' || c == '-'))
            {
                if (descUpper.Length < 5 && !descUpper.Any(char.IsLetter)) return true; // e.g. item "123.45", desc "%"
            }

            return false;
        }

        internal void ValidateResult(ParsedOrderDto result)
        {
            if (string.IsNullOrEmpty(result.SalesOrderNumber))
                result.ParsingErrors.Add("Sales Order Number not found.");
            if (string.IsNullOrEmpty(result.MachineSerialNumber))
                result.ParsingErrors.Add("Machine Serial Number not found.");
            if (string.IsNullOrEmpty(result.ControlSystemName))
                result.ParsingErrors.Add("Control System Name not found.");
        }
    }
}