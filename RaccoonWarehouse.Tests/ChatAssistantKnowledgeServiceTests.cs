using RaccoonWarehouse.Application.Service.ChatAssistant;
using Xunit;

namespace RaccoonWarehouse.Tests;

public sealed class ChatAssistantKnowledgeServiceTests
{
    [Theory]
    [InlineData("how to creat a new product", "products.create")]
    [InlineData("how to add stcok", "stocks.in")]
    [InlineData("create sales invoice", "invoices.sales")]
    [InlineData("create a payment voucher", "vouchers.payment")]
    [InlineData("اريد انشاء سند قبض", "vouchers.receipt")]
    [InlineData("اريد انشاء سند ادخال بضاعة", "stocks.in")]
    [InlineData("create stock out", "stocks.out")]
    [InlineData("أريد إنشاء سند قبض جديد", "vouchers.receipt")]
    [InlineData("please record money received", "vouchers.receipt")]
    [InlineData("I need to pay the vendor", "vouchers.payment")]
    [InlineData("please register incoming goods", "stocks.in")]
    [InlineData("كيفية إضافة منتج", "products.create")]
    public async Task FindTopicAsync_ShouldHandleCommonSpellingVariants(string question, string expectedId)
    {
        var service = new ChatAssistantKnowledgeService();

        var topic = await service.FindTopicAsync(question);

        Assert.NotNull(topic);
        Assert.Equal(expectedId, topic.Id);
    }

    [Fact]
    public async Task FindTopicAsync_ShouldKeepNewReceiptVoucherRequestExecutable()
    {
        var service = new ChatAssistantKnowledgeService();

        var topic = await service.FindTopicAsync("أريد إنشاء سند قبض جديد");

        Assert.NotNull(topic);
        Assert.Equal("vouchers.receipt", topic.Id);
        Assert.False(topic.IsAmbiguous);
        Assert.Equal("Vouchers.Receipt", topic.ActionKey);
    }

    [Fact]
    public async Task FindTopicAsync_ShouldFindSpecificInvoiceSearchWorkflow()
    {
        var service = new ChatAssistantKnowledgeService();

        var topic = await service.FindTopicAsync("how do I search invoice details");

        Assert.NotNull(topic);
        Assert.Equal("invoices.search", topic.Id);
    }

    [Fact]
    public async Task FindTopicAsync_ShouldReturnNullForUnknownQuestion()
    {
        var service = new ChatAssistantKnowledgeService();

        var topic = await service.FindTopicAsync("what is the weather tomorrow");

        Assert.Null(topic);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("اهلا")]
    public async Task FindTopicAsync_ShouldLeaveGreetingsForTheGreetingHandler(string question)
    {
        var service = new ChatAssistantKnowledgeService();

        var topic = await service.FindTopicAsync(question);

        Assert.Null(topic);
    }
}
