using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WALLnutClient
{
    public abstract class BaseMainControlViewModel : BaseControlViewModel
    {
        IModule currentModule;
        Type currentModuleType;
        ObservableCollection<BaseControlViewModel> modelCollection = new ObservableCollection<BaseControlViewModel>();

        public EventHandler OnEventFloat;
        public EventHandler OnEventClose;



        public BaseMainControlViewModel()
        {
            IsPersistentModule = true;
            modelCollection.CollectionChanged += modelCollection_CollectionChanged;
        }


        void modelCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
            {
                foreach (BaseControlViewModel model in e.NewItems)
                {
                    model.RequestClose += this.OnModelRequestClose;
                    model.RequestFloat += this.OnModelRequestFloat;
                }
            }
            if (e.OldItems != null && e.OldItems.Count != 0)
            {
                foreach (BaseControlViewModel model in e.OldItems)
                {
                    model.RequestClose -= this.OnModelRequestClose;
                    model.RequestFloat -= this.OnModelRequestFloat;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (BaseControlViewModel item in e.OldItems)
                {
                    RaiseBeforeViewDisappear(item.View);
                    RaiseAfterViewDisappear(item.View);
                    item.Dispose();
                }
                OnPropertyChanged(new PropertyChangedEventArgs("ModelCollection"));
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (BaseControlViewModel item in e.NewItems)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("ModelCollection"));
                }
            }
        }

        void OnModelRequestClose(object sender, EventArgs e)
        {
            try
            {
                BaseControlViewModel model = sender as BaseControlViewModel;
                if (model != null)
                {
                    model.Dispose();
                    if (this.ModelCollection != null && this.ModelCollection.Contains(model))
                        this.ModelCollection.Remove(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (this.OnEventClose != null)
                    this.OnEventClose(sender, e);
            }
        }

        void OnModelRequestFloat(object sender, EventArgs e)
        {
            if (this.OnEventFloat != null)
                this.OnEventFloat(sender, e);
        }

        public override void EndInit()
        {
            base.EndInit();
            CurrentModuleType = GetType();
            CurrentModule = this;
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();

            foreach (var model in ModelCollection)
                model.Dispose();
            ModelCollection.CollectionChanged -= modelCollection_CollectionChanged;
            ModelCollection.Clear();
            ModelCollection = null;

            if (OnEventFloat != null) OnEventFloat = null;
            if (OnEventClose != null) OnEventClose = null;
        }

        public void ShowModule<TModule>(object parameter) where TModule : class, IModule, new()
        {
            CurrentModuleType = typeof(TModule);
            CurrentModule = ModulesManager.CreateModule<TModule>(null, this, parameter);
            if (!ModelCollection.Contains(CurrentModule))
                ModelCollection.Add((BaseControlViewModel)CurrentModule);
        }
        public void ShowModule(Type moduleType, object parameter)
        {
            if (moduleType == null) return;
            CurrentModuleType = moduleType;
            CurrentModule = ModulesManager.CreateModule(moduleType, null, this, parameter);
        }

        public Type CurrentModuleType
        {
            get { return currentModuleType; }
            set { SetProperty<Type>(ref currentModuleType, value); }
        }
        public IModule CurrentModule
        {
            get { return currentModule; }
            set { SetProperty<IModule>(ref currentModule, value); }
        }
        public ObservableCollection<BaseControlViewModel> ModelCollection
        {
            get { return modelCollection; }
            set { SetProperty<ObservableCollection<BaseControlViewModel>>(ref modelCollection, value); }
        }

        protected virtual bool ViewIsReadyToAppear(object view)
        {
            IControlView v = view as IControlView;
            return v == null ? true : v.ViewIsReadyToAppear;
        }
        protected virtual void SetViewIsVisible(object view, bool value)
        {
            IControlView v = view as IControlView;
            if (v != null)
                v.SetViewIsVisible(value);
        }
        protected virtual void RaiseBeforeViewDisappear(object view)
        {
            IControlView v = view as IControlView;
            if (v != null)
                v.RaiseBeforeViewDisappear();
        }
        protected virtual void RaiseAfterViewDisappear(object view)
        {
            IControlView v = view as IControlView;
            if (v != null)
                v.RaiseAfterViewDisappear();
        }

        protected virtual ICommand CreateShowModuleCommand<T>() where T : class, IModule, new()
        {
            return new ExtendedActionCommand(p => ShowModule<T>(p), this, "ModelCollection", x => ModelCollection.OfType<T>().Count() == 0, null);
        }
        protected virtual ICommand CreateShowModuleCommand<T>(object param) where T : class, IModule, new()
        {
            return new ExtendedActionCommand(p => ShowModule<T>(p), this, "ModelCollection", x => ModelCollection.OfType<T>().Count() == 0, param);
        }
    }
}
