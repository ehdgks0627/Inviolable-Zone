using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

namespace WALLnutClient
{
    public class BaseActionCommand : ICommand
    {
        bool allowExecute = true;
        public BaseActionCommand(Action<object> action, object owner)
        {
            Action = action;
            Owner = owner;
        }
        public bool AllowExecute
        {
            get { return allowExecute; }
            protected set
            {
                allowExecute = value;
                RaiseAllowExecuteChanged();
            }
        }
        public Action<object> Action { get; private set; }
        protected object Owner { get; private set; }
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) { return AllowExecute; }
        public void Execute(object parameter)
        {
            if (Action != null)
                Action(parameter);
        }
        void RaiseAllowExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
    }
    public class BaseExtendedActionCommand : BaseActionCommand
    {
        string allowExecutePropertyName;
        PropertyInfo allowExecuteProperty;
        public BaseExtendedActionCommand(Action<object> action, INotifyPropertyChanged owner, string allowExecuteProperty)
            : base(action, owner)
        {
            this.allowExecutePropertyName = allowExecuteProperty;
            if (Owner != null)
            {
                this.allowExecuteProperty = Owner.GetType().GetProperty(this.allowExecutePropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (this.allowExecuteProperty == null)
                    throw new ArgumentOutOfRangeException("allowExecuteProperty");
                ((INotifyPropertyChanged)Owner).PropertyChanged += OnOwnerPropertyChanged;
            }
        }
        protected virtual void UpdateAllowExecute()
        {
            AllowExecute = Owner == null ? true : (bool)this.allowExecuteProperty.GetValue(Owner, null);
        }
        void OnOwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == this.allowExecutePropertyName)
                UpdateAllowExecute();
        }
    }
    public class ActionCommand : BaseExtendedActionCommand
    {
        public ActionCommand(Action<object> action, INotifyPropertyChanged owner, string allowExecuteProperty)
            : base(action, owner, allowExecuteProperty)
        {
            UpdateAllowExecute();
        }
    }
    public class ExtendedActionCommand : BaseExtendedActionCommand
    {
        Func<object, bool> allowExecuteCallback;
        object id;
        public ExtendedActionCommand(Action<object> action, INotifyPropertyChanged owner, string allowExecuteProperty, Func<object, bool> allowExecuteCallback, object id)
            : base(action, owner, allowExecuteProperty)
        {
            this.allowExecuteCallback = allowExecuteCallback;
            this.id = id;
            UpdateAllowExecute();
        }
        protected override void UpdateAllowExecute()
        {
            AllowExecute = this.allowExecuteCallback == null ? true : this.allowExecuteCallback(this.id);
        }
    }


    public class ExecuteCommand : ICommand
    {
        private Action<object> execute;

        private Predicate<object> canExecute;

        private event EventHandler CanExecuteChangedInternal;

        public ExecuteCommand(Action<object> execute)
            : this(execute, DefaultCanExecute)
        {
        }

        public ExecuteCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                this.CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                this.CanExecuteChangedInternal -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute != null && this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChangedInternal;
            if (handler != null)
            {
                //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        public void Destroy()
        {
            this.canExecute = _ => false;
            this.execute = _ => { return; };
        }

        private static bool DefaultCanExecute(object parameter)
        {
            return true;
        }
    }
}
