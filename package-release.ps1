param([string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$releaseRoot = Join-Path $PSScriptRoot ('publish/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$sourceRoot = Join-Path $releaseRoot 'source'
$playerRoot = Join-Path $releaseRoot 'GhostBlade-Windows-x64'
New-Item -ItemType Directory -Path $sourceRoot,$playerRoot,(Join-Path $sourceRoot 'ThreeD'),(Join-Path $sourceRoot 'assets'),(Join-Path $sourceRoot 'local-audio'),(Join-Path $playerRoot 'assets'),(Join-Path $playerRoot 'local-audio') | Out-Null
if($ExecutablePath){Copy-Item -LiteralPath (Resolve-Path -LiteralPath $ExecutablePath) -Destination (Join-Path $playerRoot 'BioSwordsman3D.exe')}else{& (Join-Path $PSScriptRoot 'build-3d.ps1') -OutputPath (Join-Path $playerRoot 'BioSwordsman3D.exe')}
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'ThreeD') -Filter '*.cs' | Copy-Item -Destination (Join-Path $sourceRoot 'ThreeD')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build-3d.ps1') -Destination $sourceRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'import-local-voice.ps1') -Destination $sourceRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'package-playtested.ps1'),(Join-Path $PSScriptRoot 'package-release.ps1') -Destination $sourceRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'publish-ignore.txt') -Destination (Join-Path $sourceRoot '.gitignore')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'local-audio/streak-1.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-2.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-3.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-4.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-5.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-6.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-7.wav'),(Join-Path $PSScriptRoot 'local-audio/streak-8.wav'),(Join-Path $PSScriptRoot 'local-audio/mascot-laugh.wav'),(Join-Path $PSScriptRoot 'local-audio/mascot-cow.wav'),(Join-Path $PSScriptRoot 'local-audio/mascot-chubby.wav') -Destination (Join-Path $sourceRoot 'local-audio')
Copy-Item -Path (Join-Path $sourceRoot 'local-audio/*') -Destination (Join-Path $playerRoot 'local-audio')
foreach ($releaseDir in @($sourceRoot,$playerRoot)) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RELEASE-README.md') -Destination (Join-Path $releaseDir 'README.md')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/combat-atlas-v1.png'),(Join-Path $PSScriptRoot 'assets/README.md') -Destination (Join-Path $releaseDir 'assets')
}
Compress-Archive -LiteralPath $playerRoot -DestinationPath (Join-Path $releaseRoot 'GhostBlade-Windows-x64.zip')
Compress-Archive -LiteralPath $sourceRoot -DestinationPath (Join-Path $releaseRoot 'GhostBlade-Source.zip')
Write-Output $releaseRoot
