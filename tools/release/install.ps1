# Rogue Legacy (unofficial mod) - player install script.
# Copies the game's Content folder from YOUR Steam copy of Rogue Legacy next to this mod.
# The game files are never modified and are not included in the mod archive.
#
# Usage: right-click > "Run with PowerShell", or:
#   powershell -ExecutionPolicy Bypass -File install.ps1 [-GameDir "D:\SteamLibrary\steamapps\common\Rogue Legacy"]
param([string]$GameDir)

$ErrorActionPreference = "Stop"
$modDir = $PSScriptRoot

function Find-RogueLegacy {
    $candidates = @("C:\Program Files (x86)\Steam\steamapps\common\Rogue Legacy")
    # Other Steam libraries (libraryfolders.vdf)
    $steamPath = (Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -Name SteamPath -ErrorAction SilentlyContinue).SteamPath
    if ($steamPath) {
        $vdf = Join-Path $steamPath "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $candidates += (Join-Path ($m.Groups[1].Value -replace '\\\\', '\') "steamapps\common\Rogue Legacy")
            }
        }
    }
    foreach ($c in $candidates) { if (Test-Path (Join-Path $c "Content\GameSpritesheets")) { return $c } }
    return $null
}

if (-not $GameDir) { $GameDir = Find-RogueLegacy }
if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir "Content\GameSpritesheets"))) {
    Write-Host "Rogue Legacy was not found automatically."
    $GameDir = Read-Host "Path of your Rogue Legacy install folder (the one containing 'Content')"
}
$source = Join-Path $GameDir "Content"
if (-not (Test-Path (Join-Path $source "GameSpritesheets"))) { throw "No game Content found in '$GameDir'." }

# 1. Game Content (copied, never moved). The mod's own Content\Mods folder is kept.
$target = Join-Path $modDir "Content"
if (Test-Path (Join-Path $target "GameSpritesheets")) {
    Write-Host "Game Content already present, skipping copy."
} else {
    Write-Host "Copying game Content from $source (~320 MB)..."
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item (Join-Path $source "*") $target -Recurse -Force -Exclude "Mods"
}

# 2. Separate save slot for the mod, seeded with a COPY of your vanilla saves (first install only).
$savedGames = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "SavedGames\RogueLegacy"
$vanillaSaves = Join-Path $savedGames "RogueLegacyStorageContainer"
$modSaves = Join-Path $savedGames "{SAVE_CONTAINER}"   # filled in by make-release.ps1 (per mod)
if ((Test-Path $vanillaSaves) -and -not (Test-Path $modSaves)) {
    Write-Host "Copying your saves to the mod's own save slot (your Steam saves are not touched)..."
    Copy-Item $vanillaSaves $modSaves -Recurse
    Get-ChildItem $modSaves -Recurse -Filter "steam_autocloud.vdf" | Remove-Item
}

Write-Host ""
Write-Host "Done! Launch RogueLegacy.exe from this folder: $modDir"
Write-Host "Tip: in Steam, 'Add a Non-Steam Game' > RogueLegacy.exe to launch it from your library."
