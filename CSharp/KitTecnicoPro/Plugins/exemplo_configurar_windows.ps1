# Plugin: Configuracoes Recomendadas do Windows
# Descrição: Aplica configuracoes recomendadas para pos-formatacao
# Autor: Kit Técnico Pro

Write-Host "=== Configuracoes Recomendadas do Windows ===" -ForegroundColor Cyan

# Desabilita animacoes para melhor desempenho
Write-Host "Otimizando animacoes visuais..." -ForegroundColor Yellow
Set-ItemProperty -Path "HKCU:\Control Panel\Desktop\WindowMetrics" -Name "MinAnimate" -Value "0" -ErrorAction SilentlyContinue

# Ativa atualizacoes automaticas
Write-Host "Verificando Windows Update..." -ForegroundColor Yellow
try {
    $wuService = Get-Service -Name "wuauserv" -ErrorAction SilentlyContinue
    if ($wuService) {
        Start-Service -Name "wuauserv" -ErrorAction SilentlyContinue
        Write-Host "Windows Update ativado!" -ForegroundColor Green
    }
} catch {
    Write-Host "Aviso: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Habilita modo de alto desempenho
Write-Host "Ativando modo de alto desempenho..." -ForegroundColor Yellow
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c -ErrorAction SilentlyContinue
Write-Host "Modo de alto desempenho ativado!" -ForegroundColor Green

Write-Host "=== Configuracoes aplicadas com sucesso! ===" -ForegroundColor Cyan
