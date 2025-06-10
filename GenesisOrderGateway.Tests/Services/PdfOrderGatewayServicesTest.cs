// Suggested location: GenesisOrderGateway.Tests/Services/PdfOrderGatewayServiceTests.cs
using FluentAssertions;
using RuleArchitect.Abstractions.DTOs.Order;   // To access ParsedOrderDto
using GenesisOrderGateway.Services; // To access PdfOrderGatewayService
using NUnit.Framework;
using System;
using System.Collections.Generic; // Required for List<ParsedLineItemDto>
using System.IO; // Required for Environment.NewLine
using System.Linq;
using System.Threading.Tasks;

namespace GenesisOrderGateway.Tests.Services
{
    [TestFixture]
    public class PdfOrderGatewayServiceTests
    {
        private PdfOrderGatewayService _service;
        private string _testPdfPath;

        [SetUp]
        public void Setup()
        {
            _service = new PdfOrderGatewayService();
            _testPdfPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Order.pdf");
            Assert.That(File.Exists(_testPdfPath), Is.True);
        }

        #region ValidateResult Tests

        [Test]
        public void ValidateResult_WhenSalesOrderNumberIsNull_AddsError()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                MachineSerialNumber = "SN123",
                ControlSystemName = "P300L",
                LineItems = new List<ParsedLineItemDto> { new ParsedLineItemDto() }
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().Contain("Sales Order Number not found.");
        }

        [Test]
        public void ValidateResult_WhenMachineSerialNumberIsNull_AddsError()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                SalesOrderNumber = "SO123",
                ControlSystemName = "P300L",
                LineItems = new List<ParsedLineItemDto> { new ParsedLineItemDto() }
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().Contain("Machine Serial Number not found.");
        }

        [Test]
        public void ValidateResult_WhenControlSystemNameIsNull_AddsError()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                SalesOrderNumber = "SO123",
                MachineSerialNumber = "SN123",
                LineItems = new List<ParsedLineItemDto> { new ParsedLineItemDto() }
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().Contain("Control System Name not found.");
        }

        [Test]
        public void ValidateResult_WhenLineItemsAreEmptyAndNoOtherErrors_DoesNotAddLineItemSpecificErrorByDefault()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                SalesOrderNumber = "SO123",
                MachineSerialNumber = "SN123",
                ControlSystemName = "P300L",
                LineItems = new List<ParsedLineItemDto>()
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().NotContainMatch("*line items*");
        }

        [Test]
        public void ValidateResult_WhenLineItemsAreEmptyAndOtherFieldsMissing_AddsOtherErrorsButNotLineItemError()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                SalesOrderNumber = null,
                MachineSerialNumber = "SN123",
                ControlSystemName = "P300L",
                LineItems = new List<ParsedLineItemDto>()
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().Contain("Sales Order Number not found.");
            dto.ParsingErrors.Should().NotContainMatch("*line items*");
        }


        [Test]
        public void ValidateResult_WhenAllFieldsAreValidAndLineItemsExist_ParsingErrorsIsEmpty()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                SalesOrderNumber = "SO123",
                MachineSerialNumber = "SN123",
                ControlSystemName = "P300L",
                LineItems = new List<ParsedLineItemDto> { new ParsedLineItemDto { SoftwareOptionNumber = "OPT001" } }
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().BeEmpty();
        }

        [Test]
        public void ValidateResult_WhenMultipleFieldsAreNull_AddsMultipleErrors()
        {
            // Arrange
            var dto = new ParsedOrderDto
            {
                LineItems = new List<ParsedLineItemDto> { new ParsedLineItemDto() }
            };

            // Act
            _service.ValidateResult(dto);

            // Assert
            dto.ParsingErrors.Should().Contain("Sales Order Number not found.");
            dto.ParsingErrors.Should().Contain("Machine Serial Number not found.");
            dto.ParsingErrors.Should().Contain("Control System Name not found.");
            dto.ParsingErrors.Should().HaveCount(3);
        }
        #endregion

        #region FindSalesOrderNumber Tests
        [Test]
        [TestCase("Some text Sales Order : 12345 more text", "12345")]
        [TestCase("Sales Order:67890", "67890")]
        [TestCase("SALES ORDER : 001122", "001122")]
        [TestCase("No order here", null)]
        [TestCase("Sales Order : ", null)] // Value is missing
        [TestCase("Sales Order : ABC", null)] // Value is not digits
        public void FindSalesOrderNumber_GivenText_ExtractsCorrectly(string inputText, string expectedOrderNumber)
        {
            // Arrange
            var dto = new ParsedOrderDto();

            // Act
            _service.FindSalesOrderNumber(inputText, dto);

            // Assert
            dto.SalesOrderNumber.Should().Be(expectedOrderNumber);
        }

        [Test]
        public void FindSalesOrderNumber_WhenAlreadyFound_DoesNotOverwrite()
        {
            // Arrange
            var dto = new ParsedOrderDto { SalesOrderNumber = "PREVIOUS123" };
            string inputText = "Some text Sales Order : 67890 more text";

            // Act
            _service.FindSalesOrderNumber(inputText, dto);

            // Assert
            dto.SalesOrderNumber.Should().Be("PREVIOUS123");
        }
        #endregion

        #region FindMachineSerialNumber Tests
        [Test]
        [TestCase("Serial # : 5A4.227006 end", "5A4.227006")]
        [TestCase("serial # :XYZ-123.A", "XYZ-123.A")]
        [TestCase("Serial#:ABC.123-DEF", "ABC.123-DEF")]
        [TestCase("No serial here", null)]
        [TestCase("Serial # : ", null)] // Value is missing
        public void FindMachineSerialNumber_GivenText_ExtractsCorrectly(string inputText, string expectedSerialNumber)
        {
            // Arrange
            var dto = new ParsedOrderDto();

            // Act
            _service.FindMachineSerialNumber(inputText, dto);

            // Assert
            dto.MachineSerialNumber.Should().Be(expectedSerialNumber);
        }

        [Test]
        public void FindMachineSerialNumber_WhenAlreadyFound_DoesNotOverwrite()
        {
            // Arrange
            var dto = new ParsedOrderDto { MachineSerialNumber = "PREVIOUS_SN" };
            string inputText = "Serial # : NEW_SN123";

            // Act
            _service.FindMachineSerialNumber(inputText, dto);

            // Assert
            dto.MachineSerialNumber.Should().Be("PREVIOUS_SN");
        }
        #endregion

        #region FindControlSystem Tests

        // Remove or comment out the old [TestCase] attributes for FindControlSystem_GivenText_ExtractsCorrectly
        // and replace with these new ones that reflect the new regex.

        [Test]
        [TestCase("Control Type : OSP-P300LA Warranty something else", "OSP-P300LA", TestName = "FindControlSystem_FollowedByWarranty")]
        [TestCase("Control Type : OSP-P300LA:OtherData", "OSP-P300LA", TestName = "FindControlSystem_FollowedByColon")]
        [TestCase("CONTROL TYPE:P500S :MoreData", "P500S", TestName = "FindControlSystem_FollowedByColon_NoSpace")]
        [TestCase("Control Type : MY-CS-01 $ some currency", "MY-CS-01", TestName = "FindControlSystem_FollowedBySpaceDollar")]
        [TestCase("Control Type : OSP-P300LA-EXTENDED Warranty", "OSP-P300LA-EXTENDED", TestName = "FindControlSystem_HyphenInName_FollowedByWarranty")]
        [TestCase("Control Type : OSP-P300LA    Warranty", "OSP-P300LA", TestName = "FindControlSystem_SpacesBeforeWarranty")]
        [TestCase("Control Type : OSP-P300LA", null, TestName = "FindControlSystem_NoValidTerminator_EndOfStr")]
        [TestCase("Control Type : OSP-P300LA and more", null, TestName = "FindControlSystem_NoValidTerminator_TextAfter")]
        [TestCase("No control type here", null, TestName = "FindControlSystem_NoControlLabel")]
        [TestCase("Control Type : ", null, TestName = "FindControlSystem_EmptyValueBeforeTerminator")] // Empty value won't be matched by ([\w\-]+?)
        [TestCase("Control Type : :EmptyBeforeColon", null, TestName = "FindControlSystem_EmptyValueBeforeColon")] // ([\w\-]+?) requires at least one char
        [TestCase("Control Type : Warranty", null, TestName = "FindControlSystem_EmptyValueBeforeWarranty")]
        public void FindControlSystem_GivenText_ExtractsBasedOnNewRegex(string inputText, string expectedControlSystem)
        {
            // Arrange
            var dto = new ParsedOrderDto();

            // Act
            // _service is initialized in your SetUp method
            _service.FindControlSystem(inputText, dto);

            // Assert
            dto.ControlSystemName.Should().Be(expectedControlSystem);
        }

        [Test] // This test remains valid as its logic is independent of the regex pattern
        public void FindControlSystem_WhenAlreadyFound_DoesNotOverwrite()
        {
            // Arrange
            var dto = new ParsedOrderDto { ControlSystemName = "PREVIOUS_CS" };
            // Input text that would normally match, to ensure it doesn't overwrite
            string inputText = "Control Type : NEW_CS-XYZ:SomeData";

            // Act
            _service.FindControlSystem(inputText, dto);

            // Assert
            dto.ControlSystemName.Should().Be("PREVIOUS_CS");
        }
        #endregion

        #region FindLineItems Tests

        [Test]
        public void FindLineItems_WithFormat1Item_NoSpace_ExtractsCorrectly()
        {
            // Arrange
            var service = new PdfOrderGatewayService(); // Instantiating directly for internal method access
            var dto = new ParsedOrderDto();
            // Item code immediately followed by description, then a data field (date)
            string inputText = "L5A40158.M8259-C53BARFEEDER INTERFACE, FOO8/4/2021";
            string expectedItemNumber = "L5A40158.M8259-C53";
            string expectedDescription = "BARFEEDER INTERFACE, FOO";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be(expectedItemNumber);
            item.Description.Should().Be(expectedDescription);
        }

        [Test]
        public void FindLineItems_WithFormat2Item_NoSpace_ExtractsCorrectly()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            // Item code immediately followed by description, then a data field (quantity/unit)
            string inputText = "RFEQ-167209-M5X0M0-3MOTOR ASSEMBLY1.00EA";
            string expectedItemNumber = "RFEQ-167209-M5X0M0-3";
            string expectedDescription = "MOTOR ASSEMBLY";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be(expectedItemNumber);
            item.Description.Should().Be(expectedDescription);
        }

        [Test]
        public void FindLineItems_WithMultipleItems_MixedFormats_NoSpaces_ExtractsAll()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            string inputText = "PrefixText L5A40158.M8259-C53ITEM ONE DESCRIPTION8/4/2021 RFEQ-167209-M5X0M0-3ITEM TWO, LONGER DESCRIPTION123.45EA SomeSuffixText";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(2);

            var item1 = dto.LineItems.FirstOrDefault(it => it.SoftwareOptionNumber == "L5A40158.M8259-C53");
            item1.Should().NotBeNull();
            item1.Description.Should().Be("ITEM ONE DESCRIPTION");

            var item2 = dto.LineItems.FirstOrDefault(it => it.SoftwareOptionNumber == "RFEQ-167209-M5X0M0-3");
            item2.Should().NotBeNull();
            item2.Description.Should().Be("ITEM TWO, LONGER DESCRIPTION");
        }

        [Test]
        public void FindLineItems_ItemFollowedByEndOfText_ExtractsCorrectly()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            string inputText = "L5A40158.M8259-C53DESCRIPTION AT THE END";
            string expectedItemNumber = "L5A40158.M8259-C53";
            string expectedDescription = "DESCRIPTION AT THE END";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be(expectedItemNumber);
            item.Description.Should().Be(expectedDescription);
        }

        [Test]
        public void FindLineItems_TextWithNoValidItemCodes_ReturnsNoItems()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            string inputText = "This is just some regular text without any item codes. Sales Order: 12345 Serial: ABC";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().BeEmpty();
        }

        [Test]
        public void FindLineItems_TextIsExactlyAHeaderPhrase_ReturnsNoItems()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            string inputText = "ITEM NUMBERDESCRIPTIONSHIP DATEQUANTITYUNITSALES PRICEDISCOUNTDISCOUNT PERCENTAMOUNT";
            // This phrase is in KnownSeparatorPhrases.
            // The CombinedItemCodeRegex won't match this directly as an item code.
            // This test ensures that if somehow a part of it was matched, IsLikelyHeaderOrJunk would catch it.
            // More directly, it tests that such text doesn't produce false positives.

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().BeEmpty();
        }

        [Test]
        public void FindLineItems_ItemFollowedByDescriptionThatIsAHeader_IsFiltered()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            // Assuming L5A40158.M8259-C53 is valid, but its description is a known header string
            string inputText = "L5A40158.M8259-C53SHIP DATE";
            // The 'IsLikelyHeaderOrJunk' should catch this because "SHIP DATE" is a KnownSeparatorPhrase

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().BeEmpty("because the description 'SHIP DATE' is a known header/separator phrase");
        }

        [Test]
        public void FindLineItems_ItemNumberIsPartOfKnownSeparatorPhrase_IsFiltered()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            // Let's imagine "SALES.ORDER-001" could be an item format, but "SALES ORDER" is in KnownSeparatorPhrases.
            // The current regex might not directly match "SALES.ORDER-001" unless it fits Format1 or Format2.
            // This test is more conceptual for the IsLikelyHeaderOrJunk logic.
            // Let's use a format that IS matched by your regex but is also a header.
            // Your current regexes are quite specific. Let's assume "PAGE-001-002-1" is a Format2 match.
            // And "PAGE" is in KnownSeparatorPhrases.
            string inputText = "PAGE-001-002-1This is a description";

            // Act
            service.FindLineItems(inputText, dto);

            // Assert
            // The IsLikelyHeaderOrJunk checks `KnownSeparatorPhrases.Contains(itemUpper)`
            // If "PAGE-001-002-1" is NOT in KnownSeparatorPhrases, this item would pass this specific check.
            // However, the check `KnownSeparatorPhrases.Any(phrase => combinedStart.StartsWith(phrase)...)` might catch it
            // if "PAGE" is a known phrase and "PAGE-001-002-1 This is a description" starts with "PAGE".

            // This test might need adjustment based on how IsLikelyHeaderOrJunk behaves with partial phrase matches.
            // For now, if "PAGE-001-002-1" is not itself a separator, it might pass.
            // If the goal is to filter if *any part* of the item number is a separator, IsLikelyHeaderOrJunk needs that logic.
            // Based on current IsLikelyHeaderOrJunk, this particular item might pass if the description is valid.
            // Let's assume it should be filtered if the item code STARTS with a known header phrase.
            // The current IsLikelyHeaderOrJunk checks `KnownSeparatorPhrases.Contains(itemUpper)` - so if `PAGE-001-002-1` isn't *exactly* in the list, it won't be filtered by that.
            // The check `KnownSeparatorPhrases.Any(phrase => combinedStart.StartsWith(phrase)...)` would filter if "PAGE" is in `KnownSeparatorPhrases`.

            dto.LineItems.Should().BeEmpty("because 'PAGE-001-002-1...' starts with 'PAGE' which is a known separator");
        }


        [Test]
        public void FindLineItems_FromUserProvidedFullText_ShouldExtractCorrectItems()
        {
            // Arrange
            var service = new PdfOrderGatewayService();
            var dto = new ParsedOrderDto();
            string fullText = "Item numberDescriptionShip dateQuantityUnitSales priceDiscountDiscount percentAmountL5A40158.M8259-C53BARFEEDER INTERFACE, UNIVERSAL TYPE, HARDWIRED8/4/20211.00EA3,147.90247.900.00%2,900.00LB3000EX/LB3000EX IIL5A40158.C-SOFTWARE2SOFTWARE PRODUCTION GROUP 28/4/20211.00EA0.000.000.00%0.00CurrencyTotalUSD2,900.00Comments : Ordered wrong barfeed on original order, need hardwired.Forwarding Agent :LTL TruckContract #     :L5A40158Serial #       :5A4.227006Model #:LB2000EX/LB2000EX IIControl Type:OSP-P300LAWarranty:226-3 YEAR LIMITED/5 YEAR CONTROLCustomer :100933Sales Order :2112939Project :1003813Order Date :07/14/2021Ref :WI00589-00Logged Order :2112939Dear Representatives,We thank you for your order. This is to confirm the specifications of your order. This product/service will be delivered and invoiced according to the delivery date and specifications of this acknowledgment unless exception is made in writing to your Okuma America Representative.Order AcknowledgementDistributor:2323 Corporate DriveWaukesha, WI 5...";

            // Act
            service.FindLineItems(fullText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);

            var item1 = dto.LineItems.FirstOrDefault(it => it.SoftwareOptionNumber == "L5A40158.M8259-C53");
            item1.Should().NotBeNull();
            item1.Description.Should().Be("BARFEEDER INTERFACE, UNIVERSAL TYPE, HARDWIRED");

        }

        #endregion

        #region Integration Tests with Actual PDF

        [Test]
        public async Task ParseOrderPdfAsync_WithActualPdfFile_PopulatesDtoCorrectly()
        {
            // Arrange
            // The _testPdfPath is set up in the Setup method.
            // You need to know the expected values from 'YourSampleOrder.pdf'.
            string expectedSalesOrder = "2112939"; // <--- CHANGE TO YOUR PDF'S VALUE
            string expectedSerialNumber = "5A4.227006"; // <--- CHANGE TO YOUR PDF'S VALUE
            string expectedControlSystem = "OSP-P300LA"; // <--- CHANGE TO YOUR PDF'S VALUE
            int expectedLineItemCount = 1; // <--- CHANGE TO YOUR PDF'S VALUE
            string expectedFirstItemNumber = "L5A40158.M8259-C53"; // <--- CHANGE TO YOUR PDF'S VALUE

            // Act
            ParsedOrderDto result = await _service.ParseOrderPdfAsync(_testPdfPath);

            // Assert
            result.Should().NotBeNull();
            result.ParsingErrors.Should().BeEmpty("The PDF should parse without errors.");

            result.SalesOrderNumber.Should().Be(expectedSalesOrder);
            result.MachineSerialNumber.Should().Be(expectedSerialNumber);
            result.ControlSystemName.Should().Be(expectedControlSystem);

            result.LineItems.Should().NotBeNull();
            result.LineItems.Should().HaveCount(expectedLineItemCount);

            // Example: Check the first line item (adjust as needed)
            if (result.LineItems.Any())
            {
                result.LineItems.First().SoftwareOptionNumber.Should().Be(expectedFirstItemNumber);
                // Add more assertions for descriptions, etc.
            }
        }
        #endregion

        // Example of how you might test ParseOrderPdfAsync if you could mock PdfDocument.Open
        // This is more of an integration test or requires refactoring the service for IPdfDocument abstraction.
        // [Test]
        // public async Task ParseOrderPdfAsync_IntegrationStyle_PopulatesDtoCorrectly()
        // {
        //     // Arrange
        //     // This would involve creating a dummy PDF stream or file with known content.
        //     // For a unit test of the Find... methods, this is not necessary.
        //     // string dummyPdfContent = "Sales Order : 123\nSerial # : SN456\nControl Type : P300\nITEM1 DESC1\nITEM2 DESC2";
        //     // var stream = new MemoryStream(Encoding.UTF8.GetBytes(dummyPdfContent)); // This is NOT a PDF stream
        //
        //     // To truly test this, you'd need a way to mock the 'PdfDocument.Open(stream)' and 'page.Text'
        //     // For now, we assume the Find... methods are tested in isolation.
        //
        //     // Act
        //     // ParsedOrderDto result = await _service.ParseOrderPdfAsync(stream);
        //
        //     // Assert
        //     // result.SalesOrderNumber.Should().Be("123");
        //     // result.MachineSerialNumber.Should().Be("SN456");
        //     // result.ControlSystemName.Should().Be("P300");
        //     // result.LineItems.Should().HaveCount(2);
        // }
    }
}
