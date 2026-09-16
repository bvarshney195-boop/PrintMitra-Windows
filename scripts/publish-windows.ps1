$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot "src\PrintMitra\PrintMitra.csproj"
$output = Join-Path $projectRoot "artifacts\publish\win-x64"

dotnet restore $project
dotnet publish $project -c Release -r win-x64 --self-contained true -o $output
Write-Host "Published to $output"
