param(
    [Parameter(Mandatory = $true)]
    [string]$SourceConnectionString,

    [Parameter(Mandatory = $true)]
    [string]$TargetConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-DataTable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,

        [Parameter(Mandatory = $true)]
        [string]$Sql
    )

    $connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 300
        $command.CommandText = $Sql
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        $table = [System.Data.DataTable]::new()
        [void]$adapter.Fill($table)
        return ,$table
    }
    finally {
        $connection.Close()
    }
}

function Invoke-TargetNonQuery {
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlConnection]$Connection,

        [Parameter(Mandatory = $false)]
        [System.Data.SqlClient.SqlTransaction]$Transaction,

        [Parameter(Mandatory = $true)]
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandTimeout = 300
    $command.CommandText = $Sql
    if ($Transaction -ne $null) {
        $command.Transaction = $Transaction
    }

    [void]$command.ExecuteNonQuery()
}

function Write-BulkTable {
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlConnection]$Connection,

        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlTransaction]$Transaction,

        [Parameter(Mandatory = $true)]
        [string]$DestinationTable,

        [Parameter(Mandatory = $true)]
        [System.Data.DataTable]$Data
    )

    if ($Data.Rows.Count -eq 0) {
        Write-Host "Skipping $DestinationTable because source row count is 0."
        return
    }

    $bulkCopy = [System.Data.SqlClient.SqlBulkCopy]::new(
        $Connection,
        [System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity,
        $Transaction)
    $bulkCopy.DestinationTableName = "dbo.$DestinationTable"
    $bulkCopy.BatchSize = 1000
    $bulkCopy.BulkCopyTimeout = 300

    foreach ($column in $Data.Columns) {
        [void]$bulkCopy.ColumnMappings.Add($column.ColumnName, $column.ColumnName)
    }

    $bulkCopy.WriteToServer($Data)
    $bulkCopy.Close()
    Write-Host "Inserted $($Data.Rows.Count) rows into $DestinationTable."
}

$sourceQueries = [ordered]@{
    Category = @'
SELECT
    Id,
    ISNULL(Name, N'') AS Name,
    Description,
    ImageUrl,
    CreatedDate,
    UpdatedDate
FROM dbo.Category
ORDER BY Id;
'@
    SubCategory = @'
SELECT
    Id,
    ISNULL(Name, N'') AS Name,
    ImageUrl,
    Description,
    ParentCategoryId,
    CreatedDate,
    UpdatedDate
FROM dbo.SubCategory
ORDER BY Id;
'@
    Brand = @'
SELECT
    Id,
    ISNULL(Name, N'') AS Name,
    ImageUrl,
    CreatedDate,
    UpdatedDate
FROM dbo.Brand
ORDER BY Id;
'@
    SubCategoryBrand = @'
SELECT
    Id,
    SubCategoryId,
    BrandId,
    CreatedDate,
    UpdatedDate
FROM dbo.SubCategoryBrand
ORDER BY Id;
'@
    Unit = @'
WITH UnitNames AS (
    SELECT DISTINCT NULLIF(LTRIM(RTRIM(Unit)), N'') AS UnitName
    FROM dbo.ProductUnit
)
SELECT
    ROW_NUMBER() OVER (ORDER BY UnitName) AS Id,
    UnitName AS Name,
    SYSUTCDATETIME() AS CreatedDate,
    SYSUTCDATETIME() AS UpdatedDate
FROM UnitNames
WHERE UnitName IS NOT NULL
ORDER BY Id;
'@
    Product = @'
SELECT
    Id,
    Name,
    ITEMCODE,
    ImageUrl,
    Description,
    CAST(NULL AS int) AS Status,
    CAST(0 AS bit) AS TaxExempt,
    CAST(NULL AS decimal(18,2)) AS MiniQuantity,
    SubCategoryId,
    BrandId,
    CreatedDate,
    UpdatedDate,
    EndDate,
    IsSoldOut,
    CAST(0 AS bit) AS IsDeleted,
    CAST(16 AS decimal(18,2)) AS TaxRate
FROM dbo.Product
ORDER BY Id;
'@
    ProductUnit = @'
WITH UnitMap AS (
    SELECT
        UnitName,
        ROW_NUMBER() OVER (ORDER BY UnitName) AS RoccoUnitId
    FROM (
        SELECT DISTINCT NULLIF(LTRIM(RTRIM(Unit)), N'') AS UnitName
        FROM dbo.ProductUnit
    ) u
    WHERE UnitName IS NOT NULL
),
ProductUnitNumbered AS (
    SELECT
        pu.*,
        ROW_NUMBER() OVER (PARTITION BY pu.ProductId ORDER BY pu.IsDefault DESC, pu.Id) AS ProductUnitOrder
    FROM dbo.ProductUnit pu
)
SELECT
    pu.Id,
    pu.Price AS SalePrice,
    CAST(0 AS decimal(18,2)) AS PurchasePrice,
    CASE WHEN ISNULL(pu.Size, 0) <= 0 THEN CAST(1 AS decimal(18,2)) ELSE pu.Size END AS QuantityPerUnit,
    pu.ProductId,
    um.RoccoUnitId AS UnitId,
    CAST(NULL AS int) AS UnitId1,
    pu.CreatedDate,
    pu.UpdatedDate,
    pu.Price AS UnTaxedPrice,
    CASE WHEN pu.ProductUnitOrder = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsBaseUnit,
    pu.IsDefault AS IsDefaultSaleUnit,
    pu.IsDefault AS IsDefaultPurchaseUnit
FROM ProductUnitNumbered pu
JOIN UnitMap um ON um.UnitName = NULLIF(LTRIM(RTRIM(pu.Unit)), N'')
ORDER BY pu.Id;
'@
}

$targetTables = @('Category', 'SubCategory', 'Brand', 'SubCategoryBrand', 'Unit', 'Product', 'ProductUnit')
$targetCountSql = @"
SELECT t.name AS TableName, SUM(p.rows) AS [Rows]
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name IN ('Category','SubCategory','Brand','SubCategoryBrand','Product','ProductUnit','Unit')
GROUP BY t.name
ORDER BY t.name;
"@

Write-Host 'Reading source catalog data...'
$sourceData = [ordered]@{}
foreach ($key in $sourceQueries.Keys) {
    $sourceData[$key] = New-DataTable -ConnectionString $SourceConnectionString -Sql $sourceQueries[$key]
    Write-Host "Source $key rows: $($sourceData[$key].Rows.Count)"
}

$targetConnection = [System.Data.SqlClient.SqlConnection]::new($TargetConnectionString)
$targetConnection.Open()
try {
    $preCounts = New-DataTable -ConnectionString $TargetConnectionString -Sql $targetCountSql
    Write-Host 'Target counts before sync:'
    $preCounts | Format-Table -AutoSize | Out-String -Width 160 | Write-Host

    foreach ($row in $preCounts.Rows) {
        if ([int64]$row['Rows'] -ne 0) {
            throw "Target table $($row['TableName']) is not empty. Stop sync to avoid duplicates/overwrite."
        }
    }

    $transaction = $targetConnection.BeginTransaction()
    try {
        foreach ($table in $targetTables) {
            Write-BulkTable -Connection $targetConnection -Transaction $transaction -DestinationTable $table -Data $sourceData[$table]
        }

        foreach ($table in $targetTables) {
            Invoke-TargetNonQuery -Connection $targetConnection -Transaction $transaction -Sql "DBCC CHECKIDENT ([$table], RESEED) WITH NO_INFOMSGS;"
        }

        $transaction.Commit()
        Write-Host 'Catalog sync committed.'
    }
    catch {
        $transaction.Rollback()
        throw
    }

    $postCounts = New-DataTable -ConnectionString $TargetConnectionString -Sql $targetCountSql
    Write-Host 'Target counts after sync:'
    $postCounts | Format-Table -AutoSize | Out-String -Width 160 | Write-Host
}
finally {
    $targetConnection.Close()
}
