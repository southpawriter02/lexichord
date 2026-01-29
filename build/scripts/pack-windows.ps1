#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packages Lexichord for Windows using Velopack.

.DESCRIPTION
    This script publishes Lexichord as a self-contained Windows application
    and packages it using Velopack for distribution and auto-update support.

.PARAMETER Version
    The version number for this release (e.g., "0.1.7").

.PARAMETER Channel
    The update channel ("stable" or "insider"). Default: "stable"

.PARAMETER OutputDir
    The output directory for packaged artifacts. Default: "./releases"

.PARAMETER Configuration
    Build configuration ("Debug" or "Release"). Default: "Release"

.PARAMETER Runtime
    Target runtime identifier. Default: "win-x64"

.EXAMPLE
    .\pack-windows.ps1 -Version "0.1.7" -Channel "stable"

.NOTES
    Requires:
    - .NET 9 SDK
    - Velopack CLI (vpk): Install with 'dotnet tool install -g vpk'

    Version: v0.1.7a
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter()]
    [ValidateSet("stable", "insider")]
    [string]$Channel = "stable",

    [Parameter()]
    [string]$OutputDir = "./releases",

    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter()]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

# Resolve paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "../..")
$HostProject = Join-Path $RepoRoot "src/Lexichord.Host/Lexichord.Host.csproj"
$PublishDir = Join-Path $RepoRoot "publish/$Runtime"
$OutputPath = Join-Path $RepoRoot $OutputDir

Write-Host "=== Lexichord Windows Packaging ===" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Channel: $Channel"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host ""

# Step 1: Clean previous publish
Write-Host "[1/4] Cleaning previous build..." -ForegroundColor Yellow
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

# Step 2: Publish as self-contained
Write-Host "[2/4] Publishing Lexichord..." -ForegroundColor Yellow
dotnet publish $HostProject `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $PublishDir `
    -p:Version=$Version `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Step 3: Verify vpk is installed
Write-Host "[3/4] Checking Velopack CLI..." -ForegroundColor Yellow
$vpkVersion = & vpk --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Velopack CLI (vpk) is not installed. Install with: dotnet tool install -g vpk"
    exit 1
}
Write-Host "Using Velopack: $vpkVersion"

# Step 4: Package with Velopack
Write-Host "[4/4] Packaging with Velopack..." -ForegroundColor Yellow
$PackageId = "Lexichord"
$PackageTitle = "Lexichord"
$ChannelSuffix = if ($Channel -eq "insider") { "-insider" } else { "" }

# Create output directory
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# Run vpk pack
vpk pack `
    --packId "$PackageId$ChannelSuffix" `
    --packVersion $Version `
    --packDir $PublishDir `
    --mainExe "Lexichord.Host.exe" `
    --outputDir $OutputPath `
    --packTitle $PackageTitle

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Packaging Complete ===" -ForegroundColor Green
Write-Host "Output: $OutputPath"
Write-Host "Files:"
Get-ChildItem $OutputPath -Filter "*.nupkg" | ForEach-Object { Write-Host "  - $($_.Name)" }
Get-ChildItem $OutputPath -Filter "*.exe" | ForEach-Object { Write-Host "  - $($_.Name)" }
