$proj = Join-Path $PSScriptRoot "KitTecnicoPro\KitTecnicoPro.csproj"
dotnet build $proj -c Debug --nologo -v q
if ($LASTEXITCODE -eq 0) {
    Start-Process (Join-Path $PSScriptRoot "KitTecnicoPro\bin\Debug\net8.0-windows\KitTecnicoPro.exe")
} else {
    Write-Host "[ERRO] Build falhou." -ForegroundColor Red
}
