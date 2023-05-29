using System;
using System.Windows.Input;

namespace DisasmPeApp.Commands
{
    internal class CommandHandler : ICommand
    {
        private Action _executeAction;
        private Func<bool>? _canExecuteFunc;

        public CommandHandler(Action executeAction, Func<bool> canExecuteFunc)
        {
            _executeAction = executeAction;
            _canExecuteFunc = canExecuteFunc;
        }

        public CommandHandler(Action executeAction)
        {
            _executeAction = executeAction;
            _canExecuteFunc = null;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecuteFunc?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _executeAction?.Invoke();
        }
    }
}
