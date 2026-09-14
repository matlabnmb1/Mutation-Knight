param([string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$sourceExe = if ($ExecutablePath) { (Resolve-Path -LiteralPath $ExecutablePath).Path } else { Join-Path $PSScriptRoot 'BioSwordsman3D.exe' }
$releaseRoot = Join-Path $PSScriptRoot ('publish/' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-playtested')
$playerRoot = Join-Path $releaseRoot 'GhostBlade-Windows-x64'
New-Item -ItemType Directory -Path $playerRoot,(Join-Path $playerRoot 'assets'),(Join-Path $playerRoot 'local-audio') | Out-Null
# Copy the played executable byte-for-byte rather than rebuilding it.
Copy-Item -LiteralPath $sourceExe -Destination (Join-Path $playerRoot 'BioSwordsman3D.exe')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/combat-atlas-v1.png') -Destination (Join-Path $playerRoot 'assets')
foreach ($streak in 1..8) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "local-audio/streak-$streak.wav") -Destination (Join-Path $playerRoot 'local-audio')
}
foreach ($mascot in 'mascot-laugh.wav','mascot-cow.wav','mascot-chubby.wav') {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "local-audio/$mascot") -Destination (Join-Path $playerRoot 'local-audio')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PLAYTESTED-README.md') -Destination (Join-Path $playerRoot 'README.md')
foreach ($relative in @('assets/combat-atlas-v1.png') + @(1..8 | ForEach-Object { "local-audio/streak-$_.wav" }) + @('local-audio/mascot-laugh.wav','local-audio/mascot-cow.wav','local-audio/mascot-chubby.wav')) {
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $relative)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $playerRoot $relative)).Hash) {
        throw "Package mismatch: $relative"
    }
}
if ((Get-FileHash -LiteralPath $sourceExe).Hash -ne (Get-FileHash -LiteralPath (Join-Path $playerRoot 'BioSwordsman3D.exe')).Hash) { throw 'Package mismatch: BioSwordsman3D.exe' }
Compress-Archive -LiteralPath $playerRoot -DestinationPath (Join-Path $releaseRoot 'GhostBlade-Windows-x64-Full.zip')
Write-Output $releaseRoot
