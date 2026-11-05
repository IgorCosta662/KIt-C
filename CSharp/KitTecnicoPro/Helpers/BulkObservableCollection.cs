using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace KitTecnicoPro.Helpers
{
    /// <summary>
    /// ObservableCollection que permite substituir todo o conteúdo disparando
    /// uma única notificação (Reset), em vez de uma por item.
    /// Evita centenas de re-layouts da UI ao filtrar listas grandes.
    /// </summary>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppress;

        /// <summary>Substitui todos os itens com uma única notificação para a UI.</summary>
        public void ReplaceAll(IEnumerable<T> items)
        {
            _suppress = true;
            try
            {
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);
            }
            finally
            {
                _suppress = false;
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppress) base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppress) base.OnPropertyChanged(e);
        }
    }
}

