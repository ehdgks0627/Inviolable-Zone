using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;


namespace WALLnutClient
{
    public abstract class BindableAndDisposable : BindableBase, IDisposable
    {
        bool disposed = false;
        public BindableAndDisposable()
        {
            InitializeCommands();
        }
        ~BindableAndDisposable() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public bool Disposed { get { return disposed; } }
        public event EventHandler AfterDispose;
        protected virtual void DisposeManaged() { }
        protected virtual void DisposeUnmanaged() { }
        void Dispose(bool disposing)
        {
            if (Disposed) return;
            disposed = true;
            if (disposing)
                DisposeManaged();
            DisposeUnmanaged();
            RaiseAfterDispose();
        }
        void RaiseAfterDispose()
        {
            if (AfterDispose != null)
                AfterDispose(this, EventArgs.Empty);
            AfterDispose = null;
        }
        #region Commands
        protected virtual void InitializeCommands()
        {
            DisposeCommand = new DelegateCommand<Object>(DoDispose);
        }
        public ICommand DisposeCommand { get; private set; }
        void DoDispose(object p) { Dispose(); }
        #endregion //Commands
    }
}
