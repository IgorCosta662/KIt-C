using System.IO;
using System.Text;
using KitTecnicoPro.Models;

namespace KitTecnicoPro.Services
{
    /// <summary>Gera relatórios de instalação em formato .txt e .html.</summary>
    public class ReportService
    {
        private readonly string _reportsDir;

        public ReportService()
        {
            _reportsDir = AppPaths.LogsDir;
            Directory.CreateDirectory(_reportsDir);
        }

        /// <summary>Gera relatório em texto simples (.txt).</summary>
        public string GenerateTxt(
            List<InstallationResult> results,
            string label,
            DateTime startTime)
        {
            TimeSpan total = DateTime.Now - startTime;
            int ok   = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            var sb = new StringBuilder();
            sb.AppendLine("════════════════════════════════════════════════");
            sb.AppendLine("  KIT TÉCNICO PRO — RELATÓRIO DE INSTALAÇÃO");
            sb.AppendLine("════════════════════════════════════════════════");
            sb.AppendLine($"  Operação   : {label}");
            sb.AppendLine($"  Data/Hora  : {startTime:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"  Máquina    : {Environment.MachineName} ({Environment.UserName})");
            sb.AppendLine($"  Duração    : {total:hh\\:mm\\:ss}");
            sb.AppendLine($"  Total      : {results.Count} programas");
            sb.AppendLine($"  Sucesso    : {ok}  |  Falha: {fail}");
            sb.AppendLine("════════════════════════════════════════════════");
            sb.AppendLine();

            sb.AppendLine("DETALHAMENTO:");
            sb.AppendLine(new string('─', 60));

            foreach (var r in results.OrderByDescending(r => r.Success))
            {
                string icon   = r.Success ? "✅" : "❌";
                string dur    = $"{r.Duration.TotalSeconds:F1}s";
                sb.AppendLine($"  {icon}  {r.AppName,-30}  {r.Message,-25}  ({dur})");
            }

            sb.AppendLine();
            sb.AppendLine("════════════════════════════════════════════════");
            sb.AppendLine("  Gerado por Kit Técnico Pro v1.0");
            sb.AppendLine("════════════════════════════════════════════════");

            string path = Path.Combine(_reportsDir, $"relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        /// <summary>Gera relatório estilizado em HTML.</summary>
        public string GenerateHtml(
            List<InstallationResult> results,
            string label,
            DateTime startTime)
        {
            TimeSpan total = DateTime.Now - startTime;
            int ok   = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);
            double pct = results.Count > 0 ? ok * 100.0 / results.Count : 0;

            var rows = new StringBuilder();
            foreach (var r in results.OrderByDescending(x => x.Success))
            {
                string bg   = r.Success ? "#1B5E20" : "#B71C1C";
                string icon = r.Success ? "✅" : "❌";
                rows.AppendLine($@"
                <tr>
                    <td style='padding:8px 12px;'>{icon} {System.Net.WebUtility.HtmlEncode(r.AppName)}</td>
                    <td style='padding:8px 12px; color:{(r.Success ? "#A5D6A7" : "#EF9A9A")};'>{System.Net.WebUtility.HtmlEncode(r.Message)}</td>
                    <td style='padding:8px 12px; text-align:center;'>{r.Duration.TotalSeconds:F1}s</td>
                    <td style='padding:8px 12px; text-align:center;'>
                        <span style='background:{bg};padding:2px 10px;border-radius:12px;font-size:11px;'>
                            {(r.Success ? "SUCESSO" : "FALHA")}
                        </span>
                    </td>
                </tr>");
            }

            string html = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
<meta charset=""UTF-8""/>
<title>Kit Técnico Pro — Relatório</title>
<style>
  body {{ font-family: 'Segoe UI', sans-serif; background: #0D1117; color: #ECEFF1; margin: 0; padding: 24px; }}
  .header {{ background: linear-gradient(135deg,#0D47A1,#1565C0); border-radius: 12px; padding: 24px; margin-bottom: 24px; }}
  .header h1 {{ margin:0; font-size:24px; }} .header p {{ margin:4px 0; opacity:.8; font-size:13px; }}
  .stats {{ display:flex; gap:16px; margin-bottom:24px; flex-wrap:wrap; }}
  .stat {{ background:#1E2A3A; border-radius:8px; padding:16px 24px; flex:1; min-width:120px; text-align:center; }}
  .stat .val {{ font-size:32px; font-weight:bold; }}
  .stat .lbl {{ font-size:12px; opacity:.7; margin-top:4px; }}
  .ok {{ color:#A5D6A7; }} .err {{ color:#EF9A9A; }} .pct {{ color:#90CAF9; }}
  table {{ width:100%; border-collapse:collapse; background:#1E2A3A; border-radius:8px; overflow:hidden; }}
  thead {{ background:#0A1628; }}
  th {{ padding:10px 12px; text-align:left; font-size:12px; opacity:.7; text-transform:uppercase; letter-spacing:.5px; }}
  tr:nth-child(even) {{ background:#16213E; }}
  .footer {{ margin-top:24px; text-align:center; opacity:.4; font-size:12px; }}
  .bar-bg {{ background:#37474F; border-radius:4px; height:8px; margin-top:8px; }}
  .bar-fill {{ background:#1565C0; border-radius:4px; height:8px; width:{pct:F0}%; }}
</style>
</head>
<body>
<div class=""header"">
  <h1>⚡ Kit Técnico Pro — Relatório de Instalação</h1>
  <p>Operação: <strong>{System.Net.WebUtility.HtmlEncode(label)}</strong></p>
  <p>Data: {startTime:dd/MM/yyyy HH:mm:ss} &nbsp;|&nbsp; Duração: {total:hh\:mm\:ss} &nbsp;|&nbsp; Máquina: {Environment.MachineName}</p>
</div>

<div class=""stats"">
  <div class=""stat""><div class=""val"">{results.Count}</div><div class=""lbl"">Total</div></div>
  <div class=""stat""><div class=""val ok"">{ok}</div><div class=""lbl"">Sucesso</div></div>
  <div class=""stat""><div class=""val err"">{fail}</div><div class=""lbl"">Falha</div></div>
  <div class=""stat""><div class=""val pct"">{pct:F0}%</div><div class=""lbl"">Taxa de Sucesso</div>
    <div class=""bar-bg""><div class=""bar-fill""></div></div>
  </div>
</div>

<table>
  <thead><tr><th>Programa</th><th>Mensagem</th><th>Duração</th><th>Status</th></tr></thead>
  <tbody>{rows}</tbody>
</table>

<div class=""footer"">Gerado por Kit Técnico Pro v1.0 em {DateTime.Now:dd/MM/yyyy HH:mm:ss}</div>
</body>
</html>";

            string path = Path.Combine(_reportsDir, $"relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(path, html, Encoding.UTF8);
            return path;
        }
    }
}
