using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class DemoChatAssistantService : IChatAssistantService
{
    public Task<ChatMessageDto> GetResponseAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = message.Trim().ToLowerInvariant();
        var reply = text.Contains("stock") || text.Contains("مخزون")
            ? "Demo response: Stock information will appear here when live data is connected."
            : text.Contains("product") || text.Contains("صنف") || text.Contains("منتج")
                ? "Demo response: I can later search products, prices, and available quantities."
                : text.Contains("invoice") || text.Contains("فاتورة") || text.Contains("مبيعات")
                    ? "Demo response: I can later help with invoices, returns, and sales summaries."
                    : "Demo assistant: I received your message. Live answers will be added later.";
        return Task.FromResult(new ChatMessageDto { Text = reply, IsDemo = true });
    }
}
