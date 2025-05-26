// Suggested location: GenesisOrderGateway.Tests/Services/PdfOrderGatewayServiceTests.cs
using NUnit.Framework;
using FluentAssertions;
using GenesisOrderGateway.Services; // To access PdfOrderGatewayService
using GenesisOrderGateway.DTOs;   // To access ParsedOrderDto
using System.Linq;
using System.Collections.Generic; // Required for List<ParsedLineItemDto>
using System; // Required for Environment.NewLine

namespace GenesisOrderGateway.Tests.Services
{
    [TestFixture]
    public class PdfOrderGatewayServiceTests
    {
        private PdfOrderGatewayService _service;

        [SetUp]
        public void Setup()
        {
            _service = new PdfOrderGatewayService();
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
        [Test]
        [TestCase("Control Type : OSP-P300LA and more", "OSP-P300LA")]
        [TestCase("Control Type:P200S", "P200S")]
        [TestCase("CONTROL TYPE : MY-CONTROLLER-01", "MY-CONTROLLER-01")]
        [TestCase("No control type here", null)]
        [TestCase("Control Type : ", null)] // Value is missing
        public void FindControlSystem_GivenText_ExtractsCorrectly(string inputText, string expectedControlSystem)
        {
            // Arrange
            var dto = new ParsedOrderDto();

            // Act
            _service.FindControlSystem(inputText, dto);

            // Assert
            dto.ControlSystemName.Should().Be(expectedControlSystem);
        }

        [Test]
        public void FindControlSystem_WhenAlreadyFound_DoesNotOverwrite()
        {
            // Arrange
            var dto = new ParsedOrderDto { ControlSystemName = "PREVIOUS_CS" };
            string inputText = "Control Type : NEW_CS-XYZ";

            // Act
            _service.FindControlSystem(inputText, dto);

            // Assert
            dto.ControlSystemName.Should().Be("PREVIOUS_CS");
        }
        #endregion

        #region FindLineItems Tests
        [Test]
        public void FindLineItems_WithSingleLineItem_ExtractsCorrectly()
        {
            // Arrange
            var dto = new ParsedOrderDto();
            string inputText = "L5A40158.M8259-C53 BARFEEDER INTERFACE ONLY";

            // Act
            _service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be("L5A40158.M8259-C53");
            item.Description.Should().Be("BARFEEDER INTERFACE ONLY");
            item.RawText.Should().Be("L5A40158.M8259-C53 BARFEEDER INTERFACE ONLY");
        }

        [Test]
        public void FindLineItems_WithMultiLineDescription_ExtractsAndCombinesDescription()
        {
            // Arrange
            var dto = new ParsedOrderDto();
            string inputText =
                "L5A40158.M8259-C53 BARFEEDER INTERFACE," + Environment.NewLine +
                "UNIVERSAL TYPE," + Environment.NewLine +
                "HARDWIRED";

            // Act
            _service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be("L5A40158.M8259-C53");
            item.Description.Should().Be("BARFEEDER INTERFACE, UNIVERSAL TYPE, HARDWIRED");
            item.RawText.Should().Be("L5A40158.M8259-C53 BARFEEDER INTERFACE," + Environment.NewLine + "UNIVERSAL TYPE," + Environment.NewLine + "HARDWIRED");
        }

        [Test]
        public void FindLineItems_WithMultipleItemsAndMultiLineDescriptions_ExtractsAllCorrectly()
        {
            // Arrange
            var dto = new ParsedOrderDto();
            string inputText =
                "Header Text" + Environment.NewLine +
                "L5A40158.M8259-C53 BARFEEDER INTERFACE," + Environment.NewLine +
                "UNIVERSAL TYPE," + Environment.NewLine +
                "HARDWIRED" + Environment.NewLine +
                "SOME OTHER TEXT IN BETWEEN" + Environment.NewLine +
                "L5A40159.ANOTHER-ITEM ANOTHER DESCRIPTION FOR ITEM 2" + Environment.NewLine +
                "AND A SECOND LINE FOR ITEM 2." + Environment.NewLine +
                "L5A40160.THIRDITEM A SINGLE LINE THIRD ITEM" + Environment.NewLine +
                "Footer Text";

            // Act
            _service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(3);

            var item1 = dto.LineItems[0];
            item1.SoftwareOptionNumber.Should().Be("L5A40158.M8259-C53");
            item1.Description.Should().Be("BARFEEDER INTERFACE, UNIVERSAL TYPE, HARDWIRED");

            var item2 = dto.LineItems[1];
            item2.SoftwareOptionNumber.Should().Be("L5A40159.ANOTHER-ITEM");
            item2.Description.Should().Be("ANOTHER DESCRIPTION FOR ITEM 2 AND A SECOND LINE FOR ITEM 2.");

            var item3 = dto.LineItems[2];
            item3.SoftwareOptionNumber.Should().Be("L5A40160.THIRDITEM");
            item3.Description.Should().Be("A SINGLE LINE THIRD ITEM");
        }

        [Test]
        public void FindLineItems_WhenNoItemsMatchPattern_ReturnsEmptyList()
        {
            // Arrange
            var dto = new ParsedOrderDto();
            string inputText =
                "This text has no valid line item starts." + Environment.NewLine +
                "Just some random sentences.";

            // Act
            _service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().BeEmpty();
        }

        [Test]
        public void FindLineItems_WithLeadingAndTrailingSpacesOnLines_ExtractsCorrectly()
        {
            // Arrange
            var dto = new ParsedOrderDto();
            string inputText =
                "  L5A40158.M8259-C53   BARFEEDER INTERFACE,  " + Environment.NewLine +
                "  UNIVERSAL TYPE,  " + Environment.NewLine +
                "  HARDWIRED  ";

            // Act
            _service.FindLineItems(inputText, dto);

            // Assert
            dto.LineItems.Should().HaveCount(1);
            var item = dto.LineItems.First();
            item.SoftwareOptionNumber.Should().Be("L5A40158.M8259-C53");
            item.Description.Should().Be("BARFEEDER INTERFACE, UNIVERSAL TYPE, HARDWIRED");
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
