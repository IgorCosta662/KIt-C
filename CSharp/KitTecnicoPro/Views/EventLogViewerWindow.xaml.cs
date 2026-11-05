using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace KitTecnicoPro.Views
{
    public class EventLogEntry2
    {
        public string NivelIcon     { get; set; } = "";
        public string NivelColor    { get; set; } = "#ECEFF1";
        public string DataHora      { get; set; } = "";
        public long   EventId       { get; set; }
        public string Fonte         { get; set; } = "";
        public string MensagemCurta { get; set; } = "";
        public string MensagemFull  { get; set; } = "";
        public string Nivel         { get; set; } = "";
    }

    public partial class EventLogViewerWindow : Window
    {
        private List<EventLogEntry2> _all  = new();
        private string _busca = "";

        public EventLogViewerWindow()
        {
            InitializeComponent();
            this.SourceInitialized += (s, e) =>
            {
                try { KitTecnicoPro.Helpers.WindowHelper.ApplyModernBackdrop(this); } catch { }
            };
        }

        private async void OnCarregarClick(object sender, RoutedEventArgs e)
        {
            BtnCarregar.IsEnabled = false;
            TxtStatus.Text = "⏳ Carregando eventos...";
            LstEventos.ItemsSource = null;

            string logName  = (CmbLog.SelectedItem   as ComboBoxItem)?.Content?.ToString() ?? "System";
            string nivelFlt = (CmbNivel.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            int    qty      = int.TryParse((CmbQtd.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int parsed) ? parsed : 500;

            string? errorMsg = null;
            var loaded = await Task.Run(() =>
            {
                var list = new List<EventLogEntry2>(qty);
                try
                {
                    using var log = new EventLog(logName);
                    int count = log.Entries.Count;
                    for (int i = count - 1; i >= 0 && list.Count < qty; i--)
                    {
                        System.Diagnostics.EventLogEntry ev;
                        try { ev = log.Entries[i]; }
                        catch { continue; }

                        using (ev)
                        {
                            string nivel = ev.EntryType switch
                            {
                                EventLogEntryType.Error        => "Erro",
                                EventLogEntryType.Warning      => "Aviso",
                                EventLogEntryType.Information  => "Informação",
                                EventLogEntryType.FailureAudit => "Falha de Auditoria",
                                EventLogEntryType.SuccessAudit => "Sucesso de Auditoria",
                                _                              => "Outro"
                            };

                            if (nivelFlt != "Todos" && nivel != nivelFlt) continue;

                            string msg   = ev.Message ?? "";
                            string curta = msg.Length > 120 ? msg[..120] + "…" : msg;

                            list.Add(new EventLogEntry2
                            {
                                Nivel         = nivel,
                                NivelIcon     = nivel == "Erro" ? "🔴" : nivel == "Aviso" ? "🟡" : nivel.Contains("Falha") ? "⛔" : "ℹ️",
                                NivelColor    = nivel == "Erro" ? "#EF5350" : nivel == "Aviso" ? "#FFA726" : "#90A4AE",
                                DataHora      = ev.TimeGenerated.ToString("dd/MM/yyyy HH:mm:ss"),
                                EventId       = ev.InstanceId,
                                Fonte         = ev.Source ?? "",
                                MensagemCurta = curta.Replace("\n", " ").Replace("\r", ""),
                                MensagemFull  = msg
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
                return list;
            });

            _all = loaded;
            ApplyFilter();
            if (errorMsg != null)
                TxtStatus.Text = $"❌ Erro ao ler log '{logName}': {errorMsg} (Execute como Administrador para ler o log Security)";

            BtnCarregar.IsEnabled = true;
        }

        private void ApplyFilter()
        {
            if (LstEventos == null) return;
            var view = string.IsNullOrEmpty(_busca)
                ? _all
                : _all.Where(e => e.Fonte.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                               || e.MensagemCurta.Contains(_busca, StringComparison.OrdinalIgnoreCase)
                               || e.EventId.ToString().Contains(_busca, StringComparison.OrdinalIgnoreCase)).ToList();

            LstEventos.ItemsSource = view;

            int erros  = _all.Count(e => e.Nivel == "Erro");
            int avisos = _all.Count(e => e.Nivel == "Aviso");
            TxtResumo.Text = $"{_all.Count} eventos  •  🔴 {erros} erros  •  🟡 {avisos} avisos";
            TxtStatus.Text = $"Exibindo {view.Count} de {_all.Count} eventos";
        }

        private void OnBuscaChanged(object s, TextChangedEventArgs e)
        { _busca = TxtBusca.Text.Trim(); ApplyFilter(); }

        private void OnEventoSelecionado(object sender, SelectionChangedEventArgs e)
        {
            if (LstEventos.SelectedItem is not EventLogEntry2 entry) return;
            TxtDetalhe.Text =
                $"Data/Hora: {entry.DataHora}\n" +
                $"Fonte:     {entry.Fonte}\n" +
                $"EventID:   {entry.EventId}\n" +
                $"Nível:     {entry.Nivel}\n" +
                $"─────────────────────────────\n" +
                entry.MensagemFull;
        }

        private void OnExportarClick(object sender, RoutedEventArgs e)
        {
            if (_all.Count == 0) { TxtStatus.Text = "⚠ Nenhum evento para exportar."; return; }
            var sb = new StringBuilder();
            sb.AppendLine($"=== Log de Eventos — {(CmbLog.SelectedItem as ComboBoxItem)?.Content} — {DateTime.Now:dd/MM/yyyy HH:mm:ss} ===");
            sb.AppendLine();
            sb.AppendLine($"{"Data/Hora",-22} {"ID",-6} {"Nível",-12} {"Fonte",-30} Mensagem");
            sb.AppendLine(new string('-', 110));
            var source = LstEventos.ItemsSource as List<EventLogEntry2> ?? _all;
            foreach (var ev in source)
                sb.AppendLine($"{ev.DataHora,-22} {ev.EventId,-6} {ev.Nivel,-12} {ev.Fonte,-30} {ev.MensagemCurta}");
            Clipboard.SetText(sb.ToString());
            TxtStatus.Text = $"✅ {source.Count} eventos copiados para a área de transferência.";
        }
    }
}
