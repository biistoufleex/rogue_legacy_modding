# Build + prepare + launch the Rogue Legacy guns mod.
# Usage:  .\run.ps1            (build, copy missing files, launch)
#         .\run.ps1 -NoLaunch  (build + copy only)
#         .\run.ps1 -NoBuild   (launch the last build)
#         .\run.ps1 -KillRunning (close a running modded game first)
param(
    [switch]$NoBuild,
    [switch]$NoLaunch,
    [switch]$KillRunning,
    [string]$SteamDir = "C:\Program Files (x86)\Steam\steamapps\common\Rogue Legacy"
)

$ErrorActionPreference = "Stop"
$repo    = $PSScriptRoot
$outDir  = Join-Path $repo "RogueCastle\bin\x64\Debug\net40"
$fnalibs = Join-Path $repo "..\fnalibs\x64"
$fnalibsUrl = "https://github.com/FNA-XNA/fnalibs-dailies/actions (latest 'CI' run -> artifact 'fnalibs')"

# 0. A running modded game locks RogueLegacy.exe and makes the build fail
$running = Get-Process RogueLegacy -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $outDir "RogueLegacy.exe") }
if ($running -and -not $NoBuild) {
    if ($KillRunning) { $running | Stop-Process; Start-Sleep -Milliseconds 500 }
    else { throw "The modded game is still running (PID $($running.Id)). Close it, or rerun with -KillRunning." }
}

# 1. Build
if (-not $NoBuild) {
    Push-Location $repo
    try {
        dotnet build RogueLegacy.sln
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }
    } finally { Pop-Location }
}
if (-not (Test-Path (Join-Path $outDir "RogueLegacy.exe"))) { throw "RogueLegacy.exe not found in $outDir" }

# 2. Content (copied from Steam, never modified there)
# (test GameSpritesheets, not Content: the build itself creates Content\Mods for the mod sprites)
if (-not (Test-Path (Join-Path $outDir "Content\GameSpritesheets"))) {
    $steamContent = Join-Path $SteamDir "Content"
    if (-not (Test-Path $steamContent)) { throw "Steam Content not found: $steamContent (use -SteamDir)" }
    Write-Host "Copying Content from Steam install (~320 MB)..."
    New-Item -ItemType Directory -Force (Join-Path $outDir "Content") | Out-Null
    Copy-Item (Join-Path $steamContent "*") (Join-Path $outDir "Content") -Recurse -Force
}

# 3. Native FNA libs (SDL3, FNA3D, FAudio, ...)
if (-not (Test-Path $fnalibs)) { throw "fnalibs missing: put the x64 folder in $fnalibs`nDownload: $fnalibsUrl" }
Get-ChildItem $fnalibs -Filter *.dll | ForEach-Object {
    $target = Join-Path $outDir $_.Name
    if (-not (Test-Path $target)) { Copy-Item $_.FullName $target; Write-Host "Copied $($_.Name)" }
}

# 4. Seed the mod's separate save container with a COPY of the vanilla saves (first run only)
$savedGames = Join-Path ([Environment]::GetFolderPath("MyDocuments")) "SavedGames\RogueLegacy"
$vanillaSaves = Join-Path $savedGames "RogueLegacyStorageContainer"
$modSaves     = Join-Path $savedGames "RogueLegacyGunsStorageContainer"
if ((Test-Path $vanillaSaves) -and -not (Test-Path $modSaves)) {
    Write-Host "Seeding mod saves from a copy of the vanilla saves -> $modSaves"
    Copy-Item $vanillaSaves $modSaves -Recurse
    Get-ChildItem $modSaves -Recurse -Filter "steam_autocloud.vdf" | Remove-Item
}

# 5. Launch (working dir = output dir so Content/ is found)
if (-not $NoLaunch) {
    Write-Host "Launching RogueLegacy.exe ..."
    Start-Process -FilePath (Join-Path $outDir "RogueLegacy.exe") -WorkingDirectory $outDir
}
