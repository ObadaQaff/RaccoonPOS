# UI Test Cases Using Demo Data

## Seed Source
- Script: `RaccoonWarehouse.Tests/DemoData_Seed.sql`
- Login users:
  - Admin: `0799000001` / `1234`
  - Cashier: `0799000002` / `1234`

## Seeded Records (Reference)
- Categories: `Beverages`, `Snacks`
- SubCategories: `Soft Drinks`, `Fruit Juice`, `Potato Chips`
- Brands: `Raccoon Cola Co`, `Mountain Sip`, `Crunchy Bite`
- Units: `Piece`, `Box`, `Carton`
- Products:
  - `Raccoon Cola 330ml` (`ITEMCODE=1001001`)
  - `Mountain Orange Juice 1L` (`ITEMCODE=1001002`)
  - `Crunchy Chips Salt 45g` (`ITEMCODE=1001003`)
- Invoices:
  - Standard Sale: `INV-DEM-0001`
  - POS Sale: `POS-DEM-0001`
  - Purchase Invoice: `PINV-DEM-0001`
- Vouchers:
  - Receipt: `VCH-REC-0001`
  - Payment (Check): `VCH-PAY-0001`
- Check: `CHK-DEM-0001`

## Core UI Test Cases

1. Category screen
- Step: open categories list.
- Expected: `Beverages` and `Snacks` displayed.
- Step: search `Beverages`.
- Expected: one row returned.

2. SubCategory screen
- Step: open subcategory list.
- Expected: `Soft Drinks`, `Fruit Juice`, `Potato Chips`.
- Step: inspect parent category relation.
- Expected: first two under `Beverages`, third under `Snacks`.

3. Brand screen
- Step: open brands list.
- Expected: 3 seeded brands visible.

4. Unit screen
- Step: open units list.
- Expected: `Piece`, `Box`, `Carton` visible.

5. Product screen
- Step: open products list and search by `ITEMCODE=1001001`.
- Expected: `Raccoon Cola 330ml` shown.
- Step: open product details.
- Expected: tax rate and product units exist.

6. ProductUnit relation screen
- Step: check product units for `Raccoon Cola 330ml`.
- Expected: two units (`Piece`, `Box`) with default sale/purchase flags.

7. Stock screen
- Step: open stock list.
- Expected: quantities > 0 for seeded products.
- Step: verify no negative stock.
- Expected: all quantities non-negative.

8. Stock In documents
- Step: open stock-in documents.
- Expected: `STKIN-DEM-0001` exists with 3 lines.

9. Standard sales invoice screen
- Step: open invoice search and load `INV-DEM-0001`.
- Expected: invoice lines load and totals match saved values.

10. POS invoice flow
- Step: open POS and search barcode `1001001`.
- Expected: cola item appears.
- Step: complete payment (cash/visa/etc).
- Expected: invoice saved, stock reduced, print available.

11. Purchase invoice screen
- Step: open purchase invoice search and load `PINV-DEM-0001`.
- Expected: supplier invoice displays with lines.

12. Voucher screen
- Step: open voucher search and find `VCH-REC-0001` and `VCH-PAY-0001`.
- Expected: both vouchers visible with correct type and amount.

13. Check payment validation
- Step: open `VCH-PAY-0001` details.
- Expected: check `CHK-DEM-0001` linked with amount `28.000`.

14. Financial transactions screen
- Step: filter transactions by source vouchers/invoices.
- Expected: seeded rows `FT-POS-0001`, `FT-SALE-0001`, `FT-REC-0001`, `FT-PAY-0001` visible.

## Negative UI Cases
1. POS: enter invalid barcode `999999999`.
- Expected: no crash, warning/empty result behavior only.

2. POS: attempt payment with empty invoice.
- Expected: blocked by validation.

3. Voucher: create check payment with duplicate check number.
- Expected: duplicate validation error.

4. Stock out: attempt quantity greater than available.
- Expected: blocked with validation message.
