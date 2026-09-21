param(
    [string]$Configuration = "Release",
    [string]$Project = "src/BaGetter/BaGetter.csproj",
    [string]$PublishProfile = "FolderProfile"
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing $Project with profile $PublishProfile ($Configuration)..."
dotnet publish $Project -c $Configuration -p:PublishProfile=$PublishProfile

$projectDirectory = Split-Path -Parent $Project
$publishDirectory = Join-Path $projectDirectory "bin\$Configuration\net10.0\publish"
$webConfigPath = Join-Path $publishDirectory "web.config"

if (-not (Test-Path $publishDirectory)) {
    throw "Publish output folder not found: $publishDirectory"
}

if (-not (Test-Path $webConfigPath)) {
    throw "Missing web.config in publish output: $webConfigPath"
}

Write-Host "Publish output is ready:"
Write-Host "  $publishDirectory"
Write-Host "web.config exists and IIS deployment prerequisites are satisfied."
