# Orders And Box Integration

Last verified: 2026-06-13

## Purpose

The integration imports pending Box carts as local endpoint-order invoices,
reserves stock, tracks operational status, posts accounting on completion, and
synchronizes status changes back to Box.

## Entry Points

- Dashboard pending-order polling and badge
- `RaccoonWarehouse-master/Orders/ImportOrder.*`
- `RaccoonWarehouse-master/Orders/OrdersTable.*`
- `RaccoonWarehouse-master/Orders/OrderInvoiceDetails.*`

## Workflow

1. Dashboard polls pending carts every 10 seconds without importing.
2. Opening the Import Order page previews pending Box carts and can explicitly import them.
3. Opening Orders or refreshing explicitly imports pending carts.
4. Product matching normalizes numeric barcodes and removes leading zeros.
5. Every cart line must map to a product and usable unit or the cart is skipped.
6. Missing customers are created as customer users.
7. Import creates a credit endpoint-order invoice with `Unknown` status.
8. Stock is reserved immediately.
9. Completion posts accounting without deducting stock again.
10. Cancellation reverses accounting and restores reserved stock.
11. Supported status changes are sent back to Box.

## Rules

- Deduplication key: `BOX-CART-{cartId}` in `Invoice.OriginalInvoiceId`.
- Statuses: `Unknown`, `InProcess`, `Completed`, and `Cancelled`.
- Local line editing is limited to editable statuses.
- Failed re-reservation restores old lines and stock.
- Local line edits do not update Box item details.
- Box status mapping is `Unknown=0`, `Completed=1`, `InProcess=2`, `Cancelled=3`.

## Services And Models

- `IBoxCartApiService`: external HTTP contract.
- `IBoxOrderImportService`: import and mapping.
- `IEndpointOrderStatusService`: stock, status, edits, and accounting.
- `RaccoonWarehouse.Domain/Orders/DTOs/BoxOrderImportDto.cs`: API/domain DTOs.
- Local persistence uses invoices, lines, customers, stock lots, and transactions.

## Configuration

`RaccoonWarehouse-master/appsettings.json` contains Box base URL and timeout.
The API client is singleton; import/status services are scoped. No API
authentication configuration was found.

## Errors And Localization

Explicit refresh shows loading and falls back to local orders after Box failure.
The Import Order page shows loading for pending-cart preview and import, disables repeated actions during the operation, and shows received/imported/existing/skipped counts plus import errors.
Background polling failures are ignored. Stock shortage errors include detailed
cart/product/unit quantities. Some translation text is stale or encoding-damaged.

## Verification

- `RaccoonWarehouse.Tests/BoxCartApiServiceTests.cs`
- `RaccoonWarehouse.Tests/BoxOrderImportServiceTests.cs`
- `RaccoonWarehouse.Tests/EndpointOrderStatusServiceTests.cs`
- `RaccoonWarehouse.Tests/QA_Testing_Summary.md`

Live Box behavior remains **Needs verification** because the server is external.

## Known Risks

- No visible API authentication.
- Auto-created customers receive a shared plaintext placeholder password.
- Process-local locks do not prevent duplicates across application instances.
- Local status may commit before Box synchronization fails.
- Multi-step import, edit, and status operations lack one explicit transaction.

## Related Paths

- `RaccoonWarehouse-master/Home/Dashboard.xaml.cs`
- `RaccoonWarehouse.Application/Service/Orders/BoxCartApiService.cs`
- `RaccoonWarehouse.Application/Service/Orders/BoxOrderImportService.cs`
- `RaccoonWarehouse.Application/Service/Orders/EndpointOrderStatusService.cs`
- `RaccoonWarehouse.Domain/Orders/`
- `RaccoonWarehouse.Domain/Invoices/`
