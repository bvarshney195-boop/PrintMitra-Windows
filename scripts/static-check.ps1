$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$required = @(
  "PrintMitra.sln",
  "src\PrintMitra\PrintMitra.csproj",
  "src\PrintMitra\MainWindow.xaml",
  "src\PrintMitra\MainWindow.xaml.cs",
  "src\PrintMitra\Services\CardImageProcessor.cs",
  "src\PrintMitra\Services\PrinterService.cs",
  "src\PrintMitra\Services\PrintLayoutService.cs"
)
foreach ($path in $required) {
  $full = Join-Path $projectRoot $path
  if (-not (Test-Path $full)) { throw "Missing required file: $path" }
}
[xml](Get-Content (Join-Path $projectRoot "src\PrintMitra\PrintMitra.csproj")) | Out-Null
[xml](Get-Content (Join-Path $projectRoot "src\PrintMitra\MainWindow.xaml")) | Out-Null
Write-Host "Static project checks passed."
