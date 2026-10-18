# Builds a redistributable archive of the mod (e.g. for Nexus Mods), WITHOUT any Rogue Legacy game asset.
# Output: ..\release\RogueLegacy_GunsCoop_v<Version>.zip  (outside the repo)
# Usage:  powershell -ExecutionPolicy Bypass -File tools\make-release.ps1 -Version 0.1.0
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$SteamDir = "C:\Program Files (x86)\Steam\steamapps\common\Rogue Legacy"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$root = Split-Path $repo -Parent                       # folder holding FNA/, fnalibs/, RogueLegacy1/
$name = "RogueLegacy_GunsCoop_v$Version"
$outRoot = Join-Path $root "release"
$stage = Join-Path $outRoot $name
$zip = Join-Path $outRoot "$name.zip"
$bin = Join-Path $repo "RogueCastle\bin\x64\Release\net40"

# 1. Release build
Push-Location $repo
try {
    dotnet build RogueLegacy.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw "Release build failed" }
} finally { Pop-Location }

# 2. Stage
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Get-ChildItem $bin -File | Where-Object { $_.Extension -in ".exe", ".dll", ".config" } | Copy-Item -Destination $stage
foreach ($lang in Get-ChildItem $bin -Directory | Where-Object { $_.Name -ne "Content" }) {
    Copy-Item $lang.FullName (Join-Path $stage $lang.Name) -Recurse      # localized resource DLLs
}
New-Item -ItemType Directory -Force (Join-Path $stage "Content\Mods") | Out-Null
Copy-Item (Join-Path $repo "RogueCastle\ModContent\*") (Join-Path $stage "Content\Mods")
Copy-Item (Join-Path $root "fnalibs\x64\*.dll") $stage

Copy-Item (Join-Path $repo "LICENSE.md"), (Join-Path $repo "EXCEPTIONS.md") $stage
Copy-Item (Join-Path $repo "tools\release\install.ps1"), (Join-Path $repo "tools\release\THIRD_PARTY.txt") $stage
(Get-Content (Join-Path $repo "tools\release\README_MOD.txt") -Raw).Replace("{VERSION}", $Version) |
    Set-Content (Join-Path $stage "README_MOD.txt") -Encoding UTF8
Copy-Item (Join-Path $root "FNA\licenses") (Join-Path $stage "licenses") -Recurse

# 3. Safety checks: no game asset may ship (license: "You must not distribute any materials from the game")
$problems = @()
$staged = Get-ChildItem $stage -Recurse -File
$problems += $staged | Where-Object { $_.Name -like "steam_api*" } | ForEach-Object { "Steam DLL: $($_.FullName)" }
$problems += $staged | Where-Object { $_.Extension -in ".xnb", ".xwb", ".xsb", ".xgs", ".wma", ".ogg", ".wav", ".mp3" } | ForEach-Object { "Game-type asset: $($_.FullName)" }
$contentItems = Get-ChildItem (Join-Path $stage "Content")
$problems += $contentItems | Where-Object { $_.Name -ne "Mods" } | ForEach-Object { "Unexpected in Content: $($_.Name)" }

if (Test-Path $SteamDir) {
    Write-Host "Comparing against the Steam install (hashes)..."
    $gameHashes = @{}
    Get-ChildItem $SteamDir -Recurse -File | ForEach-Object { $gameHashes[(Get-FileHash $_.FullName -Algorithm SHA256).Hash] = $_.FullName }
    foreach ($f in $staged) {
        $h = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
        if ($gameHashes.ContainsKey($h)) { $problems += "Identical to game file $($gameHashes[$h]): $($f.FullName)" }
    }
} else {
    Write-Warning "Steam install not found: hash comparison against game files skipped."
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "  !! $_" -ForegroundColor Red }
    throw "Release aborted: $($problems.Count) problem(s). Nothing was zipped."
}

# 4. Zip. Entries are written one by one with "/" separators: on Windows PowerShell 5.1 both Compress-Archive
#    and ZipFile.CreateFromDirectory write "\", which Linux tools and some mod managers mishandle.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($f in $staged) {
        $entryName = $f.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
$size = "{0:N1} MB" -f ((Get-Item $zip).Length / 1MB)
Write-Host "OK: $zip ($size, $($staged.Count) files, no game asset)"
