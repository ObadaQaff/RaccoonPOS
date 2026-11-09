param(
    [string]$CsvPath = '\\Mac\Home\Downloads\purchase_invoices_all_barcodes_sale_price.csv',
    [string]$OutputPath = 'C:\Users\obadaqafisheh\Downloads\PurchaseInvoice_Import_Report.xls'
)

Add-Type -AssemblyName Microsoft.VisualBasic

function Read-Csv1256([string]$Path) {
    $parser = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser($Path, [Text.Encoding]::GetEncoding(1256))
    $parser.TextFieldType = [Microsoft.VisualBasic.FileIO.FieldType]::Delimited
    $parser.SetDelimiters(',')
    $parser.HasFieldsEnclosedInQuotes = $true
    $headers = $parser.ReadFields()
    $rows = [System.Collections.Generic.List[object]]::new()
    while (-not $parser.EndOfData) {
        $values = $parser.ReadFields()
        $item = [ordered]@{}
        for ($i = 0; $i -lt $headers.Count; $i++) {
            $item[$headers[$i].Trim()] = if ($i -lt $values.Count) { $values[$i].Trim() } else { '' }
        }
        $rows.Add([pscustomobject]$item)
    }
    $parser.Close()
    return $rows
}

function Key([string]$Value) {
    if ($null -eq $Value) { return '' }
    return -join ($Value.ToUpperInvariant().ToCharArray() | Where-Object { [char]::IsLetterOrDigit($_) })
}

function Cell([string]$Value) {
    return [System.Net.WebUtility]::HtmlEncode([string]$Value)
}

$app = Get-Content -LiteralPath 'C:\Users\obadaqafisheh\source\repos\ROCCOPOS\RaccoonWarehouse-master\appsettings.json' -Raw | ConvertFrom-Json
$csb = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $app.ConnectionStrings.DefaultConnection
$productLines = & sqlcmd -S $csb.DataSource -d $csb.InitialCatalog -U $csb.UserID -P $csb.Password -C -Q "SET NOCOUNT ON; SELECT p.Id,COALESCE(CONVERT(varchar(50),p.ITEMCODE),''),REPLACE(REPLACE(REPLACE(COALESCE(p.Name,''),CHAR(9),' '),CHAR(10),' '),CHAR(13),' '),COALESCE(pu.AlternateBarcode,'') FROM dbo.Product p LEFT JOIN dbo.ProductUnit pu ON pu.ProductId=p.Id;" -h -1 -W -s "`t" -f 65001
$products = foreach ($line in $productLines) {
    $parts = $line -split "`t", 4
    if ($parts.Count -ge 4) {
        [pscustomobject]@{ Id = $parts[0].Trim(); ITEMCODE = $parts[1].Trim(); Name = $parts[2].Trim(); AlternateBarcode = $parts[3].Trim() }
    }
}

$barcodeMap = @{}
$nameMap = @{}
foreach ($p in $products) {
    $codes = @()
    if ($null -ne $p.ITEMCODE) { $codes += ([string]$p.ITEMCODE).Trim() }
    if (-not [string]::IsNullOrWhiteSpace([string]$p.AlternateBarcode)) { $codes += ([string]$p.AlternateBarcode).Trim() }
    foreach ($code in ($codes | Where-Object { $_ } | Select-Object -Unique)) {
        if (-not $barcodeMap.ContainsKey($code)) { $barcodeMap[$code] = [System.Collections.Generic.HashSet[int]]::new() }
        [void]$barcodeMap[$code].Add([int]$p.Id)
    }
    $nameKey = Key ([string]$p.Name)
    if (-not $nameMap.ContainsKey($nameKey)) { $nameMap[$nameKey] = [System.Collections.Generic.HashSet[int]]::new() }
    [void]$nameMap[$nameKey].Add([int]$p.Id)
}

$rows = @(Read-Csv1256 $CsvPath)
$unmatched = [System.Collections.Generic.List[object]]::new()
foreach ($row in $rows) {
    $candidates = @(([string]$row.BARCODE)) + @(([string]$row.ALL_BARCODES -split '\|')) + @(([string]$row.PRODUCT_CODE))
    $candidateIds = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($candidate in ($candidates | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Select-Object -Unique)) {
        if ($barcodeMap.ContainsKey($candidate)) { foreach ($id in $barcodeMap[$candidate]) { [void]$candidateIds.Add($id) } }
    }
    $nameIds = if ($nameMap.ContainsKey((Key $row.PRODUCT_NAME))) { @($nameMap[(Key $row.PRODUCT_NAME)]) } else { @() }
    $matchType = if ($candidateIds.Count -eq 1) { 'Barcode' } elseif ($nameIds.Count -eq 1) { 'Name fallback' } elseif ($candidateIds.Count -gt 1) { 'Ambiguous barcode' } else { 'No match' }
    if ($matchType -ne 'Barcode' -and $matchType -ne 'Name fallback') {
        $unmatched.Add([pscustomobject]@{
            InvoiceId = $row.INVOICE_ID; InvoiceNumber = $row.INVOICE_NO; LineId = $row.LINE_ID
            ProductId = $row.PRODUCT_ID; ProductName = $row.PRODUCT_NAME; ProductCode = $row.PRODUCT_CODE
            Barcode = $row.BARCODE; AllBarcodes = $row.ALL_BARCODES; Quantity = $row.QTY
            CostPrice = $row.COST_PRICE; SalePrice = $row.SALE_PRICE; MatchStatus = $matchType
            ManualAction = 'Add one CSV barcode to the correct database product/unit, then rerun import'
        })
    }
}

$invoiceRows = @($rows | Group-Object INVOICE_ID | ForEach-Object {
    $g = $_.Group
    $net = @($g.INVOICE_NET_TOTAL | Select-Object -Unique)
    $tax = @($g.INVOICE_TAX | Select-Object -Unique)
    $total = @($g.INVOICE_TOTAL | Select-Object -Unique)
    $lineSum = ($g | ForEach-Object {
        ([decimal]$_.LINE_TOTAL) + ([decimal]$_.LINE_TAX)
    } | Measure-Object -Sum).Sum
    $difference = [math]::Round([decimal]$lineSum - [decimal]$net[0], 3)
    $sourceConsistent = ($net.Count -eq 1 -and $tax.Count -eq 1 -and $total.Count -eq 1)
    $status = if (-not $sourceConsistent) { 'INCONSISTENT source totals' } elseif ([math]::Abs($difference) -gt 0.01) { 'LINE CALCULATION DIFF > 0.01' } else { 'Consistent source totals' }
    [pscustomobject]@{
        InvoiceId = $_.Name; InvoiceNumber = $g[0].INVOICE_NO; Lines = $g.Count
        SourceInvoiceTotal = [decimal]$total[0]; SourceTax = [decimal]$tax[0]; SourceNetTotal = [decimal]$net[0]
        CalculatedLineTotal = [decimal]$lineSum
        RepeatedTotalsConsistent = $sourceConsistent
        LineTotalDifference = $difference
        Status = $status
    }
})

$html = [Text.StringBuilder]::new()
[void]$html.AppendLine('<html><head><meta charset="utf-8"><style>body{font-family:Segoe UI}table{border-collapse:collapse;margin-bottom:24px}th,td{border:1px solid #999;padding:4px}th{background:#d9eaf7}.bad{background:#ffd6d6}</style></head><body>')
[void]$html.AppendLine('<h1>Purchase Invoice Import Report</h1>')
[void]$html.AppendLine("<p>Source: $(Cell $CsvPath) | Rows: $($rows.Count) | Unmatched rows: $($unmatched.Count)</p>")
[void]$html.AppendLine('<h2>Products requiring manual barcode mapping</h2><table><tr><th>Invoice ID</th><th>Invoice No</th><th>Line</th><th>Product ID</th><th>Product Name</th><th>Product Code</th><th>Barcode</th><th>All Barcodes</th><th>Qty</th><th>Cost Price</th><th>Sale Price</th><th>Status</th><th>Action</th></tr>')
foreach ($r in $unmatched) { [void]$html.AppendLine("<tr class='bad'><td>$(Cell $r.InvoiceId)</td><td>$(Cell $r.InvoiceNumber)</td><td>$(Cell $r.LineId)</td><td>$(Cell $r.ProductId)</td><td>$(Cell $r.ProductName)</td><td>$(Cell $r.ProductCode)</td><td>$(Cell $r.Barcode)</td><td>$(Cell $r.AllBarcodes)</td><td>$(Cell $r.Quantity)</td><td>$(Cell $r.CostPrice)</td><td>$(Cell $r.SalePrice)</td><td>$(Cell $r.MatchStatus)</td><td>$(Cell $r.ManualAction)</td></tr>") }
[void]$html.AppendLine('</table><h2>Invoice totals check</h2><table><tr><th>Invoice ID</th><th>Invoice No</th><th>Lines</th><th>Source Invoice Total</th><th>Source Tax</th><th>Source Net Total</th><th>Calculated Line Total</th><th>Difference vs Net</th><th>Repeated Totals</th><th>Status</th></tr>')
foreach ($r in $invoiceRows) { $class = if ($r.Status -like 'INCONSISTENT*' -or $r.Status -like 'LINE CALCULATION*') { " class='bad'" } else { '' }; [void]$html.AppendLine("<tr$class><td>$(Cell $r.InvoiceId)</td><td>$(Cell $r.InvoiceNumber)</td><td>$(Cell $r.Lines)</td><td>$(Cell $r.SourceInvoiceTotal)</td><td>$(Cell $r.SourceTax)</td><td>$(Cell $r.SourceNetTotal)</td><td>$(Cell $r.CalculatedLineTotal)</td><td>$(Cell $r.LineTotalDifference)</td><td>$(Cell $r.RepeatedTotalsConsistent)</td><td>$(Cell $r.Status)</td></tr>") }
[void]$html.AppendLine('</table></body></html>')
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($OutputPath))
[IO.File]::WriteAllText($OutputPath, $html.ToString(), [Text.UTF8Encoding]::new($true))
Write-Output "REPORT=$OutputPath"
Write-Output "ROWS=$($rows.Count); UNMATCHED=$($unmatched.Count); INVOICES=$($invoiceRows.Count); INCONSISTENT=$(@($invoiceRows | Where-Object Status -like 'INCONSISTENT*').Count)"
