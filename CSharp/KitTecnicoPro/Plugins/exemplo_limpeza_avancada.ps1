# Plugin: Limpeza Avancada
# Descrição: Remove arquivos de log antigos e limpa DNS cache
# Autor: Kit Técnico Pro

Write-Host "=== Plugin: Limpeza Avancada ===" -ForegroundColor Cyan

# Limpa cache DNS
Write-Host "Limpando cache DNS..." -ForegroundColor Yellow
ipconfig /flushdns
Write-Host "Cache DNS limpo!" -ForegroundColor Green

# Limpa Prefetch do Windows
Write-Host "Limpando Prefetch..." -ForegroundColor Yellow
try {
    Remove-Item "C:\Windows\Prefetch\*" -Force -ErrorAction SilentlyContinue
    Write-Host "Prefetch limpo!" -ForegroundColor Green
} catch {
    Write-Host "Aviso: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Desativa hibernacao para liberar espaco
Write-Host "Desativando hibernacao..." -ForegroundColor Yellow
powercfg /hibernate off
Write-Host "Hibernacao desativada!" -ForegroundColor Green

Write-Host "=== Plugin concluido! ===" -ForegroundColor Cyan
