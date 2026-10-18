# Stamps a pixel-art label (e.g. "COOP", "WEAPONS" + "V1.0") in the bottom-right corner of a banner image.
# The banner itself (official Rogue Legacy art) is NOT stored in this repo: pass its path with -Source.
# Usage:
#   powershell -ExecutionPolicy Bypass -File tools\make-banner.ps1 -Source banner.png -Out coop.png -Text COOP -Palette Cyan
#   powershell -ExecutionPolicy Bypass -File tools\make-banner.ps1 -Source banner.png -Out weapons.png -Text WEAPONS -SubText V1.0 -Palette Gold
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Out,
    [Parameter(Mandatory = $true)][string]$Text,
    [string]$SubText = "",
    [ValidateSet("Cyan", "Gold")][string]$Palette = "Gold",
    [int]$Scale = 2,            # output size = source size * Scale
    [double]$TextWidth = 0.42   # main label width as a fraction of the image width
)

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

# 5x7 pixel font (only the glyphs we need). '#' = lit cell.
$font = @{
    'A' = @('.###.', '#...#', '#...#', '#####', '#...#', '#...#', '#...#')
    'C' = @('.###.', '#...#', '#....', '#....', '#....', '#...#', '.###.')
    'E' = @('#####', '#....', '#....', '####.', '#....', '#....', '#####')
    'N' = @('#...#', '##..#', '##..#', '#.#.#', '#..##', '#..##', '#...#')
    'O' = @('.###.', '#...#', '#...#', '#...#', '#...#', '#...#', '.###.')
    'P' = @('####.', '#...#', '#...#', '####.', '#....', '#....', '#....')
    'S' = @('.####', '#....', '#....', '.###.', '....#', '....#', '####.')
    'V' = @('#...#', '#...#', '#...#', '#...#', '.#.#.', '.#.#.', '..#..')
    'W' = @('#.....#', '#.....#', '#..#..#', '#..#..#', '#..#..#', '#..#..#', '.##.##.') # 2-col gaps: survive bolding
    '0' = @('.###.', '#...#', '#...#', '#...#', '#...#', '#...#', '.###.')
    '1' = @('..#..', '.##..', '..#..', '..#..', '..#..', '..#..', '.###.')
    '2' = @('.###.', '#...#', '....#', '...#.', '..#..', '.#...', '#####')
    '.' = @('.', '.', '.', '.', '.', '.', '#')
    '-' = @('...', '...', '...', '###', '...', '...', '...')
    ' ' = @('..', '..', '..', '..', '..', '..', '..')
}

$palettes = @{
    # top rows -> middle -> bottom rows (pixel-art banding), plus outline and drop shadow
    Cyan = @{ Light = '#D2FCFF'; Mid = '#3FE0FF'; Dark = '#0B86B0'; Outline = '#05060F'; Shadow = '#2A0A2E' }
    Gold = @{ Light = '#FFF4B0'; Mid = '#FFC21E'; Dark = '#D9531A'; Outline = '#05060F'; Shadow = '#2A0A2E' }
}
$pal = $palettes[$Palette]
function C($hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }

# Text -> list of lit cells (x, y) in a cell grid; 1 empty column between glyphs.
# Bold: every lit cell is doubled horizontally (classic pixel-font bolding), so glyphs are 1 column wider.
function Get-Cells([string]$s) {
    $cells = New-Object System.Collections.Generic.List[object]
    $seen = @{}
    $x = 0
    foreach ($ch in $s.ToUpper().ToCharArray()) {
        $g = $font[[string]$ch]
        if ($g -eq $null) { throw "No pixel glyph for '$ch'" }
        for ($row = 0; $row -lt 7; $row++) {
            for ($col = 0; $col -lt $g[$row].Length; $col++) {
                if ($g[$row][$col] -eq '#') {
                    foreach ($cx in @(($x + $col), ($x + $col + 1))) {  # parentheses: ',' binds tighter than '+'
                        if (-not $seen["$cx,$row"]) { $seen["$cx,$row"] = $true; $cells.Add(@($cx, $row)) }
                    }
                }
            }
        }
        $x += $g[0].Length + 2
    }
    return @{ Cells = $cells; Width = $x - 1; Height = 7 }
}

function Draw-Label($gfx, $label, [int]$cell, [int]$right, [int]$bottom) {
    $ox = $right - $label.Width * $cell
    $oy = $bottom - $label.Height * $cell
    $set = @{}
    foreach ($c in $label.Cells) { $set["$($c[0]),$($c[1])"] = $true }

    $shadow = New-Object System.Drawing.SolidBrush (C $pal.Shadow)
    $outline = New-Object System.Drawing.SolidBrush (C $pal.Outline)
    $bands = @((C $pal.Light), (C $pal.Light), (C $pal.Mid), (C $pal.Mid), (C $pal.Mid), (C $pal.Dark), (C $pal.Dark))

    # 1) drop shadow, 2) 1-cell black outline, 3) colour fill with horizontal bands
    foreach ($c in $label.Cells) {
        for ($dx = -1; $dx -le 2; $dx++) { for ($dy = -1; $dy -le 2; $dy++) {
            $gfx.FillRectangle($shadow, $ox + ($c[0] + $dx) * $cell, $oy + ($c[1] + $dy) * $cell, $cell, $cell) } }
    }
    foreach ($c in $label.Cells) {
        for ($dx = -1; $dx -le 1; $dx++) { for ($dy = -1; $dy -le 1; $dy++) {
            $gfx.FillRectangle($outline, $ox + ($c[0] + $dx) * $cell, $oy + ($c[1] + $dy) * $cell, $cell, $cell) } }
    }
    foreach ($c in $label.Cells) {
        $brush = New-Object System.Drawing.SolidBrush ($bands[$c[1]])
        $gfx.FillRectangle($brush, $ox + $c[0] * $cell, $oy + $c[1] * $cell, $cell, $cell)
        # small highlight on the top-left of each lit cell that has no lit cell above it
        if (-not $set["$($c[0]),$($c[1] - 1)"]) {
            $hl = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(150, 255, 255, 255))
            $gfx.FillRectangle($hl, $ox + $c[0] * $cell, $oy + $c[1] * $cell, $cell, [Math]::Max(1, [int]($cell / 4)))
        }
        $brush.Dispose()
    }
    $shadow.Dispose(); $outline.Dispose()
    return $oy   # top of the label
}

$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source))
$w = $src.Width * $Scale; $h = $src.Height * $Scale
$img = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gfx = [System.Drawing.Graphics]::FromImage($img)
$gfx.InterpolationMode = 'HighQualityBicubic'
$gfx.PixelOffsetMode = 'HighQuality'
$gfx.DrawImage($src, 0, 0, $w, $h)
$gfx.InterpolationMode = 'NearestNeighbor'
$gfx.PixelOffsetMode = 'Half'
$gfx.SmoothingMode = 'None'

$main = Get-Cells $Text
$cell = [Math]::Max(2, [int][Math]::Floor($w * $TextWidth / $main.Width))
$margin = 3 * $cell
$bottom = $h - $margin
if ($SubText) {
    $sub = Get-Cells $SubText
    $subCell = [Math]::Max(2, [int]($cell * 0.7))
    $subTop = Draw-Label $gfx $sub $subCell ($w - $margin) $bottom
    $bottom = $subTop - 2 * $subCell
}
Draw-Label $gfx $main $cell ($w - $margin) $bottom | Out-Null

$gfx.Dispose(); $src.Dispose()
$img.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$img.Dispose()
Write-Host "OK: $Out (${w}x$h, cell ${cell}px)"
