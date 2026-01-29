#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Signs Windows executables using SignTool.exe.

.DESCRIPTION
    This script signs Windows executables with a PFX certificate using SignTool
    from the Windows SDK. It includes signature verification after signing.

.PARAMETER ExePath
    Path to the executable to sign.

.PARAMETER CertificatePath
    Path to the PFX certificate file.

.PARAMETER CertificatePassword
    Password for the PFX certificate.

.PARAMETER TimestampUrl
    RFC 3161 timestamp server URL. Default: http://timestamp.digicert.com

.EXAMPLE
    .\sign-windows.ps1 -ExePath ".\Lexichord.exe" -CertificatePath ".\cert.pfx" -CertificatePassword "secret"

.NOTES
    Requires:
    - Windows SDK with SignTool.exe
    - Valid code signing certificate (PFX format)

    Version: v0.1.7b
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePath,

    [Parameter(Mandatory = $true)]
    [string]$CertificatePassword,

    [Parameter()]
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Lexichord Windows Code Signing" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Validate inputs
if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found: $ExePath"
    exit 1
}

if (-not (Test-Path $CertificatePath)) {
    Write-Error "Certificate not found: $CertificatePath"
    exit 1
}

Write-Host "Executable:  $ExePath"
Write-Host "Certificate: $CertificatePath"
Write-Host "Timestamp:   $TimestampUrl"
Write-Host ""

# Find SignTool.exe from Windows SDK
Write-Host "[1/3] Locating SignTool.exe..." -ForegroundColor Yellow

$signTool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" `
    -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue | `
    Where-Object { $_.FullName -like "*x64*" } | `
    Sort-Object { [version](Split-Path (Split-Path $_.DirectoryName -Parent) -Leaf) } -Descending | `
    Select-Object -First 1

if (-not $signTool) {
    Write-Error "SignTool.exe not found. Please install the Windows SDK."
    exit 1
}

Write-Host "Using: $($signTool.FullName)" -ForegroundColor Green
Write-Host ""

# Sign the executable
Write-Host "[2/3] Signing executable..." -ForegroundColor Yellow

$signArgs = @(
    "sign"
    "/f", $CertificatePath
    "/p", $CertificatePassword
    "/fd", "sha256"
    "/tr", $TimestampUrl
    "/td", "sha256"
    "/v"
    $ExePath
)

& $signTool.FullName @signArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Signing failed with exit code: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Signing successful!" -ForegroundColor Green
Write-Host ""

# Verify signature
Write-Host "[3/3] Verifying signature..." -ForegroundColor Yellow

& $signTool.FullName verify /pa $ExePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Signature verification failed"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Signature verification passed!" -ForegroundColor Green

# Display certificate info
Write-Host ""
Write-Host "Certificate Details:" -ForegroundColor Cyan
& $signTool.FullName verify /pa /v $ExePath 2>&1 | Select-String -Pattern "Issued|Subject|Serial|SHA" | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Signing Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
