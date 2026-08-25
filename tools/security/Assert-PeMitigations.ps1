param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path $Path).Path
$bytes = [System.IO.File]::ReadAllBytes($resolved)
if ($bytes.Length -lt 512) { throw 'PE file is unexpectedly small.' }

$peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
if ($peOffset -lt 0 -or $peOffset + 96 -ge $bytes.Length) { throw 'Invalid PE header offset.' }
if ([Text.Encoding]::ASCII.GetString($bytes, $peOffset, 4) -ne "PE`0`0") { throw 'PE signature not found.' }

$optionalHeader = $peOffset + 24
$magic = [BitConverter]::ToUInt16($bytes, $optionalHeader)
if ($magic -notin @(0x10B, 0x20B)) { throw "Unsupported PE optional-header magic: 0x$('{0:X}' -f $magic)" }

$dllCharacteristicsOffset = $optionalHeader + 0x46
$dllCharacteristics = [BitConverter]::ToUInt16($bytes, $dllCharacteristicsOffset)
$dynamicBase = ($dllCharacteristics -band 0x0040) -ne 0
$nxCompat = ($dllCharacteristics -band 0x0100) -ne 0
$highEntropyVa = ($dllCharacteristics -band 0x0020) -ne 0

$lines = @(
    "PE mitigation audit: $([IO.Path]::GetFileName($resolved))"
    "Optional header: $(if ($magic -eq 0x20B) { 'PE32+ (64-bit)' } else { 'PE32 (32-bit)' })"
    "DllCharacteristics: 0x$('{0:X4}' -f $dllCharacteristics)"
    "ASLR / DYNAMIC_BASE: $dynamicBase"
    "DEP / NX_COMPAT: $nxCompat"
    "HIGH_ENTROPY_VA: $highEntropyVa"
)

$lines | ForEach-Object { Write-Host $_ }
if ($ReportPath) {
    $parent = Split-Path -Parent $ReportPath
    if ($parent) { New-Item -ItemType Directory -Force $parent | Out-Null }
    $lines | Set-Content $ReportPath
}

if (-not $dynamicBase) { throw 'Released executable is missing DYNAMIC_BASE / ASLR compatibility.' }
if (-not $nxCompat) { throw 'Released executable is missing NX_COMPAT / DEP compatibility.' }
if ($magic -eq 0x20B -and -not $highEntropyVa) { throw 'Released x64 executable is missing HIGH_ENTROPY_VA.' }
