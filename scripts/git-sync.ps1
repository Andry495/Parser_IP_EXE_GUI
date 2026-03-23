#Requires -Version 5.1
# Синхронизация main с origin: pull --rebase + push. Коммиты не создаёт.
# Запуск из корня репозитория: .\scripts\git-sync.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$dirty = git status --porcelain
if ($dirty) {
    Write-Host "Есть незакоммиченные изменения. Сначала сделайте commit или откатите их." -ForegroundColor Yellow
    Write-Host $dirty
    exit 1
}

Write-Host "git fetch origin..."
git fetch origin
Write-Host "git pull --rebase origin main..."
git pull --rebase origin main
Write-Host "git push origin main..."
git push origin main
Write-Host "Готово."
