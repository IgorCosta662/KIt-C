using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>Representa um tweak individual do Windows.</summary>
    public class WindowsTweak
    {
        public string Id          { get; set; } = string.Empty;
        public string Nome        { get; set; } = string.Empty;
        public string Descricao   { get; set; } = string.Empty;
        public string Icone       { get; set; } = "🔧";
        public string Script      { get; set; } = string.Empty;
        public bool   IsSelected  { get; set; } = true;
        public bool   IsRisky     { get; set; }
    }

    /// <summary>Módulo de otimização e tweaks do Windows via PowerShell.</summary>
    public class OptimizationService
    {
        private readonly PowerShellService _ps;

        public OptimizationService(PowerShellService ps) => _ps = ps;

        /// <summary>Retorna todos os tweaks disponíveis.</summary>
        public static List<WindowsTweak> GetAvailableTweaks() => new()
        {
            new WindowsTweak
            {
                Id        = "show_hidden",
                Nome      = "Mostrar Arquivos Ocultos",
                Descricao = "Exibe arquivos e pastas ocultos no Explorer",
                Icone     = "👁️",
                Script    = @"Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'Hidden' -Value 1 -Force"
            },
            new WindowsTweak
            {
                Id        = "show_extensions",
                Nome      = "Mostrar Extensões de Arquivo",
                Descricao = "Exibe a extensão de todos os arquivos no Explorer",
                Icone     = "📄",
                Script    = @"Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'HideFileExt' -Value 0 -Force"
            },
            new WindowsTweak
            {
                Id        = "high_perf",
                Nome      = "Modo de Alto Desempenho",
                Descricao = "Ativa o plano de energia de alto desempenho",
                Icone     = "⚡",
                Script    = @"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
            },
            new WindowsTweak
            {
                Id        = "disable_startup_delay",
                Nome      = "Remover Delay de Inicialização",
                Descricao = "Elimina o atraso de 5s na inicialização dos apps de startup",
                Icone     = "🚀",
                Script    = @"Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize' -Name 'StartupDelayInMSec' -Value 0 -Type DWord -Force"
            },
            new WindowsTweak
            {
                Id        = "disable_cortana",
                Nome      = "Desativar Cortana",
                Descricao = "Desativa a Cortana para reduzir uso de recursos",
                Icone     = "🔇",
                Script    = @"New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search' -Force | Out-Null; Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search' -Name 'AllowCortana' -Value 0 -Type DWord -Force"
            },
            new WindowsTweak
            {
                Id        = "flush_dns",
                Nome      = "Limpar Cache DNS",
                Descricao = "Esvazia o cache DNS do sistema",
                Icone     = "🌐",
                Script    = @"ipconfig /flushdns"
            },
            new WindowsTweak
            {
                Id        = "clear_prefetch",
                Nome      = "Limpar Prefetch",
                Descricao = "Remove arquivos de Prefetch do Windows",
                Icone     = "🗑️",
                Script    = @"Remove-Item 'C:\Windows\Prefetch\*' -Force -ErrorAction SilentlyContinue; Write-Host 'Prefetch limpo.'"
            },
            new WindowsTweak
            {
                Id        = "disable_telemetry",
                Nome      = "Reduzir Telemetria",
                Descricao = "Define telemetria no nível mínimo (0 = Security)",
                Icone     = "🔒",
                Script    = @"Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force",
                IsRisky   = false
            },
            new WindowsTweak
            {
                Id        = "disable_hibernation",
                Nome      = "Desativar Hibernação",
                Descricao = "Libera o espaço ocupado pelo arquivo hiberfil.sys",
                Icone     = "💾",
                Script    = @"powercfg /hibernate off"
            },
            new WindowsTweak
            {
                Id        = "visual_perf",
                Nome      = "Otimizar Efeitos Visuais",
                Descricao = "Ajusta animações para melhor desempenho",
                Icone     = "🎨",
                Script    = @"$path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects'; if (!(Test-Path $path)) { New-Item $path -Force | Out-Null }; Set-ItemProperty -Path $path -Name 'VisualFXSetting' -Value 2 -Type DWord -Force"
            },
            new WindowsTweak
            {
                Id        = "windows_update",
                Nome      = "Verificar Windows Update",
                Descricao = "Força verificação de atualizações do Windows",
                Icone     = "🔄",
                Script    = @"(New-Object -ComObject Microsoft.Update.AutoUpdate).DetectNow()"
            },
            new WindowsTweak
            {
                Id        = "clean_winsxs",
                Nome      = "Limpeza de Componentes do Sistema",
                Descricao = "Executa DISM para limpar componentes desnecessários",
                Icone     = "🧹",
                Script    = @"Start-Process 'dism.exe' -ArgumentList '/Online /Cleanup-Image /StartComponentCleanup' -Wait -NoNewWindow",
                IsRisky   = false
            }
        };

        /// <summary>Executa um tweak pelo seu ID.</summary>
        public async Task<bool> RunTweakAsync(
            string tweakId,
            Action<string, LogLevel> log,
            CancellationToken ct = default)
        {
            var tweak = GetAvailableTweaks().FirstOrDefault(t => t.Id == tweakId);
            if (tweak == null)
            {
                log($"❌ Tweak não encontrado: {tweakId}", LogLevel.Error);
                return false;
            }
            return await RunTweakAsync(tweak, log, ct);
        }

        /// <summary>Executa um tweak diretamente.</summary>
        public async Task<bool> RunTweakAsync(
            WindowsTweak tweak,
            Action<string, LogLevel> log,
            CancellationToken ct = default)
        {
            log($"  {tweak.Icone} Aplicando: {tweak.Nome}", LogLevel.Info);
            bool ok = await _ps.ExecuteAsync(tweak.Script, log, ct);
            if (ok) log($"  ✅ {tweak.Nome} aplicado.", LogLevel.Success);
            else    log($"  ⚠️ {tweak.Nome} — pode requerer reinicialização.", LogLevel.Warning);
            return ok;
        }

        /// <summary>Executa todos os tweaks selecionados.</summary>
        public async Task<int> RunSelectedTweaksAsync(
            IEnumerable<WindowsTweak> tweaks,
            Action<string, LogLevel> log,
            CancellationToken ct = default)
        {
            var list = tweaks.ToList();
            log($"━━━ Otimização do Windows ({list.Count} tweaks) ━━━", LogLevel.Info);
            int success = 0;
            foreach (var tweak in list)
            {
                if (ct.IsCancellationRequested) break;
                if (await RunTweakAsync(tweak, log, ct)) success++;
            }
            log($"━━━ Otimização concluída: {success}/{list.Count} aplicados ━━━",
                success == list.Count ? LogLevel.Success : LogLevel.Warning);
            return success;
        }
    }
}
