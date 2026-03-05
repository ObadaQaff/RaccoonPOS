$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot "app"
$target = Join-Path $env:LOCALAPPDATA "RaccoonWarehouse"

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $source "*") -Destination $target -Recurse -Force

$shell = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcut = $shell.CreateShortcut((Join-Path $desktop "Raccoon Warehouse.lnk"))
$shortcut.TargetPath = Join-Path $target "RaccoonWarehouse.exe"
$shortcut.WorkingDirectory = $target
$shortcut.IconLocation = (Join-Path $target "RaccoonWarehouse.exe") + ",0"
$shortcut.Save()

Start-Process -FilePath (Join-Path $target "RaccoonWarehouse.exe")
