# ⚙ TechKit Pro — Kit Técnico + Pós-Formatação

Ferramenta WinForms em C# (.NET 8) para técnicos de TI.
Interface visual completa para executar diagnósticos e pós-formatação com um clique.

---

## 📋 Requisitos

| Requisito | Versão mínima |
|---|---|
| Windows | 10 / 11 |
| .NET SDK | 8.0+ |
| Visual Studio | 2022 (ou `dotnet` CLI) |
| winget | Instalado (pré-instalado no Windows 11) |

---

## 🚀 Como compilar e rodar

### Opção 1 — Visual Studio 2022
1. Abra `TechKitPro.csproj`
2. Build → Start (F5)
3. Aceite o prompt de UAC (admin necessário para SFC/DISM)

### Opção 2 — Linha de comando
```bash
dotnet restore
dotnet build -c Release
dotnet run
```

### Publicar como .exe único
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
O `.exe` ficará em `bin/Release/net8.0-windows/win-x64/publish/TechKitPro.exe`

---

## 🔧 Kit Técnico — Ferramentas

| Ferramenta | Comando executado | Admin? |
|---|---|---|
| Teste de Disco | `chkdsk C: /scan /perf` | ✖ |
| Scan de Memória | WMI Win32_PhysicalMemory | ✖ |
| Temperatura CPU | WMI MSAcpi_ThermalZoneTemperature | ✖ |
| Diagnóstico Rede | `Test-Connection 8.8.8.8` | ✖ |
| Scan Malware | `Start-MpScan -ScanType QuickScan` | ✖ |
| Integridade SFC | `sfc /scannow` | ✔ |
| Reparo DISM | `DISM /RestoreHealth` | ✔ |
| Startup | Lista itens de inicialização | ✖ |

---

## 💻 Pós-Formatação — Módulos

| Módulo | O que faz |
|---|---|
| Windows Update | Instala todas as atualizações via PSWindowsUpdate |
| Google Chrome | `winget install Google.Chrome` |
| 7-Zip | `winget install 7zip.7zip` |
| VLC | `winget install VideoLAN.VLC` |
| Adobe Acrobat | `winget install Adobe.Acrobat.Reader.64-bit` |
| WinRAR | `winget install RARLab.WinRAR` |
| TeamViewer | `winget install TeamViewer.TeamViewer` |
| Limpeza de Disco | `cleanmgr /sagerun:1` |
| Memória Virtual | Configura paginação automática |
| Ponto de Restauro | Cria checkpoint antes de encerrar |

---

## ✅ Checklist

20 itens cobrindo:
- Pré-formatação (backup)
- Sistema (Windows / ativação)
- Drivers (chipset, áudio, vídeo, rede)
- Segurança (antivírus, firewall)
- Aplicativos
- Diagnóstico (disk, SFC, temperatura)
- Finalização (restauração de dados, explicação ao cliente)

Gera relatório `.txt` assinável na área de trabalho.

---

## 📂 Estrutura do projeto

```
TechKitPro/
├── Program.cs              → Ponto de entrada
├── FormMain.cs             → UI principal (abas, botões, logs)
├── Helpers/
│   ├── SystemInfo.cs       → Leitura de hardware via WMI
│   └── ProcessRunner.cs    → Execução de cmd/PowerShell/winget
├── TechKitPro.csproj       → Projeto .NET 8 WinForms
├── app.manifest            → Elevação de privilégios (UAC)
└── README.md
```

---

## 💡 Personalizar apps instalados

Em `FormMain.cs`, na lista `_posTools`, troque os `winget` IDs:

```csharp
new("chrome", "🌐", "Google Chrome",
    "winget install Google.Chrome --silent ..."),
```

Encontre IDs em: https://winget.run

---

## ⚠ Aviso

Algumas operações (SFC, DISM, Windows Update) exigem **administrador**.
O `app.manifest` já solicita elevação na inicialização.
