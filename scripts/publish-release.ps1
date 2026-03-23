#Requires -Version 5.1
<#
  Собирает single-file publish (FDD и self-contained) и упаковывает в artifacts\*.zip
  Запуск из корня репозитория: .\scripts\publish-release.ps1
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$proj = Join-Path $root "ParserIpExeMonitor\ParserIpExeMonitor.csproj"
$out = Join-Path $root "artifacts"
New-Item -ItemType Directory -Force -Path $out | Out-Null

Write-Host "Publishing framework-dependent (single file)..."
dotnet publish $proj -c Release -r win-x64 --self-contained false -o (Join-Path $out "publish-fdd")

Write-Host "Publishing self-contained (single file, compressed)..."
dotnet publish $proj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -o (Join-Path $out "publish-selfcontained")

$zip1 = Join-Path $out "ParserIpExeMonitor-win-x64-framework-dependent.zip"
$zip2 = Join-Path $out "ParserIpExeMonitor-win-x64-self-contained.zip"
if (Test-Path $zip1) { Remove-Item $zip1 -Force }
if (Test-Path $zip2) { Remove-Item $zip2 -Force }

Compress-Archive -Path (Join-Path $out "publish-fdd\*") -DestinationPath $zip1 -Force
Compress-Archive -Path (Join-Path $out "publish-selfcontained\*") -DestinationPath $zip2 -Force

Write-Host "Done:"
Write-Host "  $zip1"
Write-Host "  $zip2"
