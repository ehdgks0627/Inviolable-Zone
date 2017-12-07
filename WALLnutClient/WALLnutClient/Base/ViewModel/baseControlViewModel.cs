using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WALLnutClient
{
    public class BaseControlViewModel : BindableAndDisposable, IModule
    {
        #region <Define>

        #region <Private>
        object _View;
        IModule _Parent;
        IModule _Main;

        //데브 Docking에서 사용함.
        bool _IsActive = false;
        bool _IsOpened;
        bool _IsClosed = false;
        bool _ShowTabHeader = true;
        bool _ShowTabHeaderPin = true;
        bool _ShowTabHeaderClose = true;
        string _DockingGroup;

        string title;
        string subTitle;
        object initParam;
        ICommand _CloseCommand;
        ICommand _FloatCommand;
        ModulesManagerInternalData internalData;
        #endregion //Private

        public bool IsPersistentModule { get; protected set; }
        public IModulesManager ModulesManager { get; protected set; }

        public event EventHandler BeforeDisappear;
        public event EventHandler BeforeAppearAsync;
        public event EventHandler BeforeAppear;
        public event EventHandler<BaseEventArgs> UpdateDataContext;
        public event EventHandler RequestClose;
        public event EventHandler RequestFloat;
        public event EventHandler RequestActive;

        /// <summary>
        /// Content
        /// </summary>
        public object View
        {
            get { return _View; }
            private set { SetProperty<object>(ref _View, value); }
        }
        public IModule Parent
        {
            get { return _Parent; }
            private set { SetProperty<IModule>(ref _Parent, value); }
        }
        public IModule Main
        {
            get { return _Main; }
            private set { SetProperty<IModule>(ref _Main, value); }
        }

        #region <Dock>
        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty<bool>(ref _IsActive, value, OnActiveChanged); }
        }
        public bool IsOpened
        {
            get { return _IsOpened; }
            set { SetProperty<bool>(ref _IsOpened, value); }
        }
        public bool IsClosed
        {
            get { return _IsClosed; }
            set { SetProperty<bool>(ref _IsClosed, value, OnIsClosedChanged); }
        }
        /// <summary>
        /// Dock Header
        /// </summary>
        public string Title
        {
            get { return title; }
            set { SetProperty<string>(ref title, value); }
        }
        /// <summary>
        /// Dock Header SubTitle
        /// </summary>
        public string SubTitle
        {
            get { return subTitle; }
            set { SetProperty<string>(ref subTitle, value); }
        }

        #region <DockLayoutManager 관련 속성>
        public String LanguageClose
        {
            get { return "닫기"; }
        }
        public String LanguageShowWindow
        {
            get { return "팝업으로 전환"; }
        }

        /// <summary>
        /// Dock Item Close Command
        /// </summary>
        public virtual ICommand CloseCommand
        {
            get
            {
                if (_CloseCommand == null)
                    _CloseCommand = new ExecuteCommand(OnRequestClose);
                return _CloseCommand;
            }
            set { _CloseCommand = value; }
        }
        public virtual ICommand FloatCommand
        {
            get
            {
                if (_FloatCommand == null)
                    _FloatCommand = new ExecuteCommand(OnRequestFloat);
                return _FloatCommand;
            }
            set { _FloatCommand = value; }
        }

        protected virtual void OnRequestClose(object param)
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        protected virtual void OnRequestFloat(object param)
        {
            EventHandler handler = this.RequestFloat;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        protected virtual void OnIsClosedChanged()
        {
            IsOpened = !IsClosed;
        }
        protected virtual void OnActiveChanged()
        {
            EventHandler handler = this.RequestActive;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        #endregion //DockLayoutManager 관련 속성

        #endregion //Dock

        #endregion //Define


        public virtual List<IModule> GetSubmodules()
        {
            return new List<IModule>();
        }
        public virtual void BeginInit() { }
        public virtual void EndInit() { }
        protected virtual void LoadData(object parameter) { }
        protected virtual void InitData(object parameter) { }
        protected virtual void SaveData() { }
        protected override void DisposeManaged()
        {
            if (View != null) (View as BaseUserControls).Dispose();
            View = null;
            Main = null;
            Parent = null;

            if (CloseCommand != null) CloseCommand = null;
            if (FloatCommand != null) FloatCommand = null;

            if (BeforeDisappear != null) BeforeDisappear = null;
            if (BeforeAppearAsync != null) BeforeAppearAsync = null;
            if (BeforeAppear != null) BeforeAppear = null;
            if (UpdateDataContext != null) UpdateDataContext = null;
            if (RequestClose != null) RequestClose = null;
            if (RequestFloat != null) RequestFloat = null;
            if (RequestActive != null) RequestActive = null;

            base.DisposeManaged();
        }

        void IModule.SetView(object v)
        {
            View = v;
        }
        void IModule.SetIsVisible(bool v)
        {
            //IsVisible = v;
        }
        void IModule.RaiseBeforeDisappear()
        {
            SaveData();
            if (BeforeDisappear != null)
                BeforeDisappear(this, EventArgs.Empty);
        }
        object IModule.InitParam
        {
            get { return initParam; }
            set { initParam = value; }
        }
        void IModule.RaiseBeforeAppearAsync()
        {
            LoadData(initParam);
            if (BeforeAppearAsync != null)
                BeforeAppearAsync(this, EventArgs.Empty);
        }
        void IModule.RaiseBeforeAppear()
        {
            InitData(initParam);
            if (BeforeAppear != null)
                BeforeAppear(this, EventArgs.Empty);
        }
        void IModule.SetModulesManager(IModulesManager v)
        {
            ModulesManager = v;
        }
        ModulesManagerInternalData IModule.ModulesManagerInternalData
        {
            get { return internalData; }
            set { internalData = value; }
        }
        void IModule.SetParent(IModule v)
        {
            Parent = v;
            Main = (v == null) ? this : v.Main;
        }
        void IModule.RaiseUpdateDataContext(object parameter)
        {
            if (UpdateDataContext != null)
                UpdateDataContext(this, new BaseEventArgs(parameter));
        }
    }
}
