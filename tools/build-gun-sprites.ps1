# Builds RogueCastle/ModContent/gunsSpritesheet.png + .xml (Rogue Legacy SpriteLibrary format)
# from the CC0 packs extracted in ../gun-assets:
#   - SnoopethDuckDuck "Guns"            (https://snoopethduckduck.itch.io/guns)            -> weapon icons
#   - Master484 "Bullet Collection 1"    (https://opengameart.org/content/bullet-collection-1-m484) -> bullets + impact sparks
# Usage: powershell -ExecutionPolicy Bypass -File tools\build-gun-sprites.ps1 [-Preview]
param([switch]$Preview)

$ErrorActionPreference = "Stop"
$repo   = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $repo "..\gun-assets"
$outDir = Join-Path $repo "RogueCastle\ModContent"
New-Item -ItemType Directory -Force $outDir | Out-Null

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class GunSpriteTool
{
    public static Bitmap Load(string path)
    {
        using (Bitmap src = new Bitmap(path))
        {
            Bitmap b = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            return b;
        }
    }

    // M484 sheet has an opaque black background: make (near) pure black transparent.
    public static void KeyBlack(Bitmap b)
    {
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                Color c = b.GetPixel(x, y);
                if (c.R < 12 && c.G < 12 && c.B < 12) b.SetPixel(x, y, Color.Transparent);
            }
    }

    // Bounding box of the 8-connected opaque blob nearest to (px, py).
    public static Rectangle Blob(Bitmap b, int px, int py)
    {
        int sx = -1, sy = -1, best = int.MaxValue;
        for (int y = Math.Max(0, py - 8); y <= Math.Min(b.Height - 1, py + 8); y++)
            for (int x = Math.Max(0, px - 8); x <= Math.Min(b.Width - 1, px + 8); x++)
                if (b.GetPixel(x, y).A > 0)
                {
                    int d = (x - px) * (x - px) + (y - py) * (y - py);
                    if (d < best) { best = d; sx = x; sy = y; }
                }
        if (sx < 0) throw new Exception("No sprite near " + px + "," + py);

        bool[,] seen = new bool[b.Width, b.Height];
        Queue<Point> q = new Queue<Point>();
        q.Enqueue(new Point(sx, sy)); seen[sx, sy] = true;
        int minX = sx, maxX = sx, minY = sy, maxY = sy;
        while (q.Count > 0)
        {
            Point p = q.Dequeue();
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = p.X + dx, ny = p.Y + dy;
                    if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height || seen[nx, ny]) continue;
                    if (b.GetPixel(nx, ny).A == 0) continue;
                    seen[nx, ny] = true; q.Enqueue(new Point(nx, ny));
                }
        }
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static Bitmap Crop(Bitmap b, Rectangle r)
    {
        Bitmap o = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(o)) g.DrawImage(b, new Rectangle(0, 0, r.Width, r.Height), r, GraphicsUnit.Pixel);
        return o;
    }
}
"@

# ---- Sources --------------------------------------------------------------------------------
$guns = Join-Path $assets "snoopeth"
$bulletSheet = [GunSpriteTool]::Load((Join-Path $assets "M484BulletCollection1.png"))
[GunSpriteTool]::KeyBlack($bulletSheet)
function Bullet($x, $y) { [GunSpriteTool]::Crop($bulletSheet, [GunSpriteTool]::Blob($bulletSheet, $x, $y)) }

# Each entry: sprite name -> list of frames; Hitbox = projectile (gets a collision rectangle).
# Names must contain a single "_" : ProjectileObj.RunDestroyAnimation() builds the impact sprite
# name with SpriteName.Replace("_", "Explosion_").
$sprites = [ordered]@{
    "GunPistolIcon_Sprite"           = @{ Frames = @([GunSpriteTool]::Load("$guns\Pack 2\2px\26.png")) }
    "GunShotgunIcon_Sprite"          = @{ Frames = @([GunSpriteTool]::Load("$guns\Pack 1\2px\51.png")) }
    "GunSMGIcon_Sprite"              = @{ Frames = @([GunSpriteTool]::Load("$guns\Pack 2\2px\9.png")) }
    "GunPistolBullet_Sprite"         = @{ Frames = @(Bullet 49 191); Hitbox = $true }   # yellow capsule
    "GunSMGBullet_Sprite"            = @{ Frames = @(Bullet 29 227); Hitbox = $true }   # small blue capsule
    "GunPellet_Sprite"               = @{ Frames = @(Bullet 75 122); Hitbox = $true }   # glowing yellow ball
    "GunPistolBulletExplosion_Sprite"= @{ Frames = @((Bullet 313 53), (Bullet 319 53), (Bullet 328 53), (Bullet 347 113)) } # yellow sparks
    "GunPelletExplosion_Sprite"      = @{ Frames = @((Bullet 313 53), (Bullet 319 53), (Bullet 328 53)) }
    "GunSMGBulletExplosion_Sprite"   = @{ Frames = @((Bullet 313 73), (Bullet 319 73), (Bullet 328 73)) }                   # cyan sparks
}

# ---- Pack (simple shelf packing, 2px padding) -----------------------------------------------
$sheetW = 256; $pad = 2; $x = $pad; $y = $pad; $rowH = 0
$placed = @()
foreach ($name in $sprites.Keys) {
    $i = 0
    foreach ($bmp in $sprites[$name].Frames) {
        if ($x + $bmp.Width + $pad -gt $sheetW) { $x = $pad; $y += $rowH + $pad; $rowH = 0 }
        $placed += [pscustomobject]@{ Name = $name; Index = $i; X = $x; Y = $y; Bmp = $bmp; Hitbox = [bool]$sprites[$name].Hitbox }
        $x += $bmp.Width + $pad; $rowH = [Math]::Max($rowH, $bmp.Height); $i++
    }
}
$sheetH = $y + $rowH + $pad
$sheet = New-Object System.Drawing.Bitmap $sheetW, $sheetH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::Transparent)
foreach ($p in $placed) { $g.DrawImage($p.Bmp, $p.X, $p.Y, $p.Bmp.Width, $p.Bmp.Height) }
$g.Dispose()
$pngPath = Join-Path $outDir "gunsSpritesheet.png"
$sheet.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# ---- XML (same schema as Content/GameSpritesheets/*.xml) ------------------------------------
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("<xml>")
foreach ($p in $placed) {
    $ax = [Math]::Floor($p.Bmp.Width / 2); $ay = [Math]::Floor($p.Bmp.Height / 2)
    [void]$sb.AppendLine("  <instance name=`"$($p.Name)`" index=`"$($p.Index)`" posX=`"$($p.X)`" posY=`"$($p.Y)`" anchorX=`"$ax`" anchorY=`"$ay`" width=`"$($p.Bmp.Width)`" height=`"$($p.Bmp.Height)`"/>")
}
foreach ($p in $placed) {
    $frame = $p.Index + 1
    if ($p.Hitbox) {
        $ax = [Math]::Floor($p.Bmp.Width / 2); $ay = [Math]::Floor($p.Bmp.Height / 2)
        [void]$sb.AppendLine("  <animation frame=`"$frame`" name=`"$($p.Name)`" index=`"$($p.Index)`" duration=`"1`" label=`"`">")
        [void]$sb.AppendLine("    <rectangle x=`"-$ax`" y=`"-$ay`" width=`"$($p.Bmp.Width)`" height=`"$($p.Bmp.Height)`" rotation=`"0`" type=`"1`"/>")
        [void]$sb.AppendLine("  </animation>")
    } else {
        [void]$sb.AppendLine("  <animation frame=`"$frame`" name=`"$($p.Name)`" index=`"$($p.Index)`" duration=`"1`" label=`"`"/>")
    }
}
[void]$sb.AppendLine("</xml>")
[System.IO.File]::WriteAllText((Join-Path $outDir "gunsSpritesheet.xml"), $sb.ToString())

$placed | ForEach-Object { "{0,-34} #{1}  {2}x{3} at ({4},{5})" -f $_.Name, $_.Index, $_.Bmp.Width, $_.Bmp.Height, $_.X, $_.Y }
"Sheet: ${sheetW}x${sheetH} -> $pngPath"

if ($Preview) {
    $k = 4
    $prev = New-Object System.Drawing.Bitmap ($sheetW * $k), ($sheetH * $k)
    $g = [System.Drawing.Graphics]::FromImage($prev)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 70, 70, 80))
    $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $g.DrawImage($sheet, 0, 0, $prev.Width, $prev.Height)
    $g.Dispose()
    $prev.Save((Join-Path $env:TEMP "gunsSpritesheet_preview.png"))
    "Preview: $(Join-Path $env:TEMP 'gunsSpritesheet_preview.png')"
}
