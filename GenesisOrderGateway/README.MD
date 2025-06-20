# GenesisOrderGateway

## Overview

GenesisOrderGateway is a specialized component designed to parse and extract structured data from PDF order documents. It acts as a gateway between raw PDF files and a structured `ParsedOrderDto` object, making order information available for further processing within the OSP Genesis Suite.

The primary implementation, `PdfOrderGatewayService`, uses the [PdfPig](https://github.com/UglyToad/PdfPig) library to read text content from PDFs and a series of regular expressions to find and extract key pieces of information.

## Key Features

* **PDF Parsing**: Reads text content from any page of a given PDF document.
* **Data Extraction**: Uses robust regular expressions to find and extract critical order data, including:
    * Sales Order Number
    * Machine Serial Number
    * Control System Name
* **Line Item Parsing**: Intelligently identifies and extracts line items from the document, capturing the software option number and its description. It is designed to handle various text formats and filter out irrelevant header/footer information.
* **Error Reporting**: Collects and reports any issues encountered during parsing (e.g., missing critical fields) in the `ParsingErrors` collection of the result object.

## Core Components

### `IGenesisOrderGateway`

This is the main interface for the component, defining the contract for any order parsing service.

```csharp
public interface IGenesisOrderGateway
{
    // Parses a PDF from a stream
    Task<ParsedOrderDto> ParseOrderPdfAsync(Stream pdfStream);

    // Parses a PDF from a file path
    Task<ParsedOrderDto> ParseOrderPdfAsync(string filePath);
}
```

### `PdfOrderGatewayService`

The concrete implementation of `IGenesisOrderGateway`. It contains the logic for opening a PDF and applying various regex patterns to extract data.

### `ParsedOrderDto`

The Data Transfer Object returned by the service. It holds the extracted information.

```csharp
public class ParsedOrderDto
{
    public string SalesOrderNumber { get; set; }
    public string MachineSerialNumber { get; set; }
    public string ControlSystemName { get; set; }
    public List<ParsedLineItemDto> LineItems { get; set; }
    public List<string> ParsingErrors { get; set; }
    public bool IsSuccess => !ParsingErrors.Any();
}
```

## Dependencies

* **PdfPig**: The core library used for reading text from PDF documents.
* **RuleArchitect.Abstractions**: For the `ParsedOrderDto` and other shared DTOs.

## Usage

The service is intended to be used via Dependency Injection. Register it in your `App.xaml.cs` or service configuration.

**1. Service Registration:**

```csharp
// In your ConfigureServices method
services.AddTransient<IGenesisOrderGateway, PdfOrderGatewayService>();
```

**2. Inject and Use:**

```csharp
public class SomeViewModel
{
    private readonly IGenesisOrderGateway _orderGateway;

    public SomeViewModel(IGenesisOrderGateway orderGateway)
    {
        _orderGateway = orderGateway;
    }

    public async Task ProcessPdfOrder(string filePath)
    {
        ParsedOrderDto parsedResult = await _orderGateway.ParseOrderPdfAsync(filePath);

        if (parsedResult.IsSuccess)
        {
            // Process the valid data
            Console.WriteLine(<span class="math-inline">"Order Number\: \{parsedResult\.SalesOrderNumber\}"\);
foreach \(var item in parsedResult\.LineItems\)
\{
Console\.WriteLine\(</span>"  - {item.SoftwareOptionNumber}: {item.Description}");
            }
        }
        else
        {
            // Handle parsing errors
            var errors = string.Join("\n", parsedResult.ParsingErrors);
            Console.WriteLine($"Failed to parse PDF: \n{errors}");
        }
    }
}
```

## How It Works

The `PdfOrderGatewayService` reads the entire text content from the PDF. It then runs a series of `internal` methods, each responsible for finding a specific piece of data:
1.  `FindSalesOrderNumber`
2.  `FindMachineSerialNumber`
3.  `FindControlSystem`
4.  `FindLineItems`

Each method uses a specifically crafted regular expression to locate and capture the relevant text. The `FindLineItems` method is the most complex, as it identifies item codes based on known formats and then intelligently captures the description text that follows, stopping before it hits known data terminators (like dates, prices, or other item codes).

The final `ValidateResult` method checks if all critical fields were found and adds errors to the DTO if any are missing.
