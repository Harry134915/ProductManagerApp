using System.ComponentModel;
using System.Windows.Input;

namespace ProductManagerApp.Infrastructure.Commands
{
    public class AsyncRelayCommand : ICommand, INotifyPropertyChanged
    {
        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool IsExecuting => _isExecuting;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                SetIsExecuting(true);
                RaiseCanExecuteChanged();
                await _execute(parameter);
            }
            finally
            {
                SetIsExecuting(false);
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetIsExecuting(bool value)
        {
            if (_isExecuting == value)
            {
                return;
            }

            _isExecuting = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(IsExecuting)));
        }
    }
}
