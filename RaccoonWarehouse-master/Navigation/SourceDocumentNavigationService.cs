using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Orders;
using RaccoonWarehouse.Stocks;
using RaccoonWarehouse.Vouchers;
using System.Reflection;
using System.Windows;

namespace RaccoonWarehouse.Navigation
{
    public class SourceDocumentNavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public SourceDocumentNavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OpenSourceDocument(string? referenceType, int? referenceId)
        {
            if (string.IsNullOrWhiteSpace(referenceType) || !referenceId.HasValue || referenceId <= 0)
                throw new InvalidOperationException("Reference type and id are required.");

            switch (referenceType.Trim())
            {
                case "Invoice":
                    OpenInvoice(referenceId.Value);
                    return;
                case "Voucher":
                    await OpenVoucherAsync(referenceId.Value);
                    return;
                case "StockDocument":
                    await OpenStockDocumentAsync(referenceId.Value);
                    return;
                default:
                    throw new InvalidOperationException($"No navigation route mapped for reference type '{referenceType}'.");
            }
        }

        private void OpenInvoice(int invoiceId)
        {
            var window = _serviceProvider.GetRequiredService<OrderInvoiceDetails>();
            window.SetInvoiceId(invoiceId);
            window.Show();
        }

        private async Task OpenVoucherAsync(int voucherId)
        {
            var voucherService = _serviceProvider.GetRequiredService<IVoucherService>();
            var voucher = await voucherService.GetByIdAsync(voucherId);
            if (!voucher.Success || voucher.Data == null)
                throw new InvalidOperationException("Voucher was not found.");

            var window = _serviceProvider.GetRequiredService<PaymentVoucher>();
            var method = typeof(PaymentVoucher).GetMethod("LoadVoucher", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(window, new object[] { voucher.Data });
            window.Show();
        }

        private async Task OpenStockDocumentAsync(int documentId)
        {
            var stockService = _serviceProvider.GetRequiredService<IStockDocumentService>();
            var stockDoc = await stockService.GetByIdAsync(documentId);
            if (!stockDoc.Success || stockDoc.Data == null)
                throw new InvalidOperationException("Stock document was not found.");

            var window = _serviceProvider.GetRequiredService<StockIn>();
            var method = typeof(StockIn).GetMethod("LoadSelectedStockIn", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(window, new object[] { stockDoc.Data });
            window.Show();
        }
    }
}
