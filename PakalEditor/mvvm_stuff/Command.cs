using System;
using System.Windows.Input;

namespace PakalEditor.mvvm_stuff
{
    public class Command<T> : ICommand
    {
        private readonly Action<T> command;
        private readonly Predicate<T> commandAllowed;

        public Command(Action<T> command, Predicate<T> commandAllowed = null)
        {
            this.command = command;
            this.commandAllowed = commandAllowed ?? (arg => true);
        }

        public void CommandAllowedChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Execute(T parameter)
        {
            ((ICommand)this).Execute(parameter);
        }
        public bool CanExecute(T parameter)
        {
            return ((ICommand)this).CanExecute(parameter);
        }
        public void ExecuteIfAllowed(T paremeter)
        {
            if (CanExecute(paremeter))
            {
                Execute(paremeter);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return commandAllowed?.Invoke((T)parameter) ?? true;
        }

        void ICommand.Execute(object parameter)
        {
            command((T)parameter);
        }

        public event EventHandler CanExecuteChanged;
    }

    public class Command : ICommand
    {
        private readonly Action command;
        private readonly Func<bool> commandAllowed;

        public Command(Action command, Func<bool> commandAllowed = null)
        {
            this.command = command;
            this.commandAllowed = commandAllowed ?? (() => true) ;
        }

        public void CommandAllowedChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return commandAllowed?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            command();
        }

        public void ExecuteIfAllowed()
        {
            if (commandAllowed())
            {
                command();
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}