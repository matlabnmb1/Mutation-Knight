param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$gameSource = Join-Path $PSScriptRoot 'ThreeD'
$gameOutput = if ($OutputPath) { $OutputPath } else { Join-Path $PSScriptRoot 'BioSwordsman3D.exe' }
$frameworkPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkPath 'csc.exe'
$gameFiles = Get-ChildItem -LiteralPath $gameSource -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $compilerPath /nologo /target:winexe /optimize+ /platform:x64 /out:$gameOutput /reference:System.dll /reference:System.Core.dll /reference:System.Xaml.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:"$frameworkPath\WPF\WindowsBase.dll" /reference:"$frameworkPath\WPF\PresentationCore.dll" /reference:"$frameworkPath\WPF\PresentationFramework.dll" $gameFiles
if ($LASTEXITCODE -ne 0) { throw "3D build failed: $LASTEXITCODE" }
Write-Output $gameOutput
