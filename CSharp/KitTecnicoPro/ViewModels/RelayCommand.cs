using System.Windows.Input;

namespace KitTecnicoPro.ViewModels
{
    /// <summary>Implementação síncrona de ICommand para binding MVVM.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Comando assíncrono moderno para MVVM: evita lambdas <c>async void</c> desprotegidas,
    /// bloqueia reentrância (cliques duplos) e encaminha falhas ao logger estruturado.
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<Exception>? _onError;
        private int _isExecuting;

        public AsyncRelayCommand(
            Func<Task> executeAsync,
            Func<bool>? canExecute = null,
            Action<Exception>? onError = null)
            : this(_ => executeAsync(), canExecute == null ? null : _ => canExecute(), onError) { }

        public AsyncRelayCommand(
            Func<object?, Task> executeAsync,
            Func<object?, bool>? canExecute = null,
            Action<Exception>? onError = null)
        {
            _executeAsync = executeAsync;
            _canExecute   = canExecute;
            _onError      = onError;
        }

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
            => Volatile.Read(ref _isExecuting) == 0 && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
                return;

            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _executeAsync(parameter);
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
            finally
            {
                Volatile.Write(ref _isExecuting, 0);
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
