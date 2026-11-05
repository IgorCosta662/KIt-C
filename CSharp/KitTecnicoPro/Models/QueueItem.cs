using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KitTecnicoPro.Models
{
    public enum QueueItemStatus { Waiting, Running, Success, Failed, Cancelled }

    /// <summary>Item da fila inteligente de instalação com prioridade e retry.</summary>
    public class QueueItem : INotifyPropertyChanged
    {
        private QueueItemStatus _status = QueueItemStatus.Waiting;
        private string _statusText      = "Aguardando";
        private int    _retryCount;

        public AppItem App           { get; set; } = null!;

        /// <summary>1 = Alta, 2 = Normal, 3 = Baixa</summary>
        public int Priority          { get; set; } = 2;

        public int MaxRetries        { get; set; } = 2;
        public TimeSpan Timeout      { get; set; } = TimeSpan.FromMinutes(15);

        public int RetryCount
        {
            get => _retryCount;
            set { _retryCount = value; OnPropertyChanged(); }
        }

        public QueueItemStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                StatusText = value switch
                {
                    QueueItemStatus.Waiting   => "⏳ Aguardando",
                    QueueItemStatus.Running   => "⚙️ Instalando...",
                    QueueItemStatus.Success   => "✅ Instalado",
                    QueueItemStatus.Failed    => "❌ Falhou",
                    QueueItemStatus.Cancelled => "⛔ Cancelado",
                    _                         => ""
                };
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string StatusColor => Status switch
        {
            QueueItemStatus.Success  => "#A5D6A7",
            QueueItemStatus.Failed   => "#EF9A9A",
            QueueItemStatus.Running  => "#FFE082",
            QueueItemStatus.Waiting  => "#78909C",
            _                        => "#546E7A"
        };

        public string PriorityLabel => Priority switch
        {
            1 => "🔴 Alta",
            2 => "🟡 Normal",
            3 => "🟢 Baixa",
            _ => "Normal"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
