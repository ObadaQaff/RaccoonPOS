using RaccoonWarehouse.Application.Service.Orders;
using Xunit;

namespace RaccoonWarehouse.Tests;

public class BoxOrderImportServiceTests
{
    [Theory]
    [InlineData("040000155188", "40000155188")]
    [InlineData(" 000123 ", "123")]
    [InlineData("0", "0")]
    [InlineData("6253002402318", "6253002402318")]
    public void NormalizeBarcode_ShouldMatchNumericRaccoonBarcodes(string input, string expected)
    {
        Assert.Equal(expected, BoxOrderImportService.NormalizeBarcode(input));
    }

    [Fact]
    public void FormatMissingItemNote_ShouldIncludeBarcodeNameQuantityAndPrice()
    {
        var note = BoxOrderImportService.FormatMissingItemNote("00123", "Missing Product", 2.5m, 4.75m);

        Assert.Contains("Barcode: 00123", note);
        Assert.Contains("Name: Missing Product", note);
        Assert.Contains("Quantity: 2.5", note);
        Assert.Contains("Price: 4.75", note);
    }}
