/*
  POS_ROW_COLOR_RULES_DATABASE_UPDATE.sql
  Safe, rerunnable configuration update. No product, stock, invoice, or accounting data is changed.
  Apply manually after backing up the active ROCCOPOS database.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AppSettings', N'U') IS NULL
    THROW 51000, 'dbo.AppSettings does not exist. Start the application once or deploy its AppSettings table first.', 1;

DECLARE @Key NVARCHAR(150) = N'Pos.RowColorRules';
DECLARE @Value NVARCHAR(MAX) = N'[
  {"Id":"expired","Name":"Expired","IsActive":true,"Priority":100,"Scope":"Both","ConditionType":"Expired","NumericValue":null,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#FDE2E2","ForegroundColor":"#9B1C1C","AccentColor":"#DC2626","Label":"Expired"},
  {"Id":"expiry-30","Name":"Expiry within 30 days","IsActive":true,"Priority":90,"Scope":"Both","ConditionType":"ExpiryWithinDays","NumericValue":30,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#FCE7F3","ForegroundColor":"#9D174D","AccentColor":"#DB2777","Label":"Near expiry"},
  {"Id":"expiry-90","Name":"Expiry within 90 days","IsActive":true,"Priority":80,"Scope":"Both","ConditionType":"ExpiryWithinDays","NumericValue":90,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#FFF1D6","ForegroundColor":"#92400E","AccentColor":"#F59E0B","Label":"Expiry warning"},
  {"Id":"discount-10","Name":"Discount 10% or more","IsActive":true,"Priority":70,"Scope":"Both","ConditionType":"DiscountAtLeast","NumericValue":10,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#E2F7E9","ForegroundColor":"#166534","AccentColor":"#16A34A","Label":"10%+ discount"},
  {"Id":"discount-5","Name":"Discount 5% or more","IsActive":true,"Priority":60,"Scope":"Both","ConditionType":"DiscountAtLeast","NumericValue":5,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#F0FDF4","ForegroundColor":"#166534","AccentColor":"#65A30D","Label":"5%+ discount"},
  {"Id":"low-stock","Name":"Low stock","IsActive":true,"Priority":40,"Scope":"Both","ConditionType":"LowStock","NumericValue":10,"CategoryId":null,"CategoryName":null,"BackgroundColor":"#FEF3C7","ForegroundColor":"#92400E","AccentColor":"#D97706","Label":"Low stock"}
]';

BEGIN TRANSACTION;
-- Seed only when absent. Existing custom rules are intentionally preserved on rerun.
IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = @Key)
BEGIN
    INSERT INTO dbo.AppSettings ([Key], [Value], [Description], [CreatedDate], [UpdatedDate])
    VALUES (@Key, @Value, N'POS and product-search row color rules.', GETDATE(), GETDATE());
END;
COMMIT TRANSACTION;

-- Validation: exactly one setting and valid JSON.
SELECT [Key], ISJSON([Value]) AS IsValidJson, [UpdatedDate]
FROM dbo.AppSettings
WHERE [Key] = @Key;
