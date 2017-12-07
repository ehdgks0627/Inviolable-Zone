using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace WALLnutClient
{
    public interface IViewsListProvider
    {
        Type GetViewType(Type moduleType);
    }

    public interface IModuleView : IControlView
    {
        void SetViewIsReadyToAppear(bool v);
    }

    public interface IViewsManager
    {
        void CreateView(IModule module);
        void ShowView(IModule module);
        IModule GetModule(object view);
    }


    public class ViewsManager : IViewsManager
    {
        IViewsListProvider viewListProvider;
        public ViewsManager(IViewsListProvider viewListProvider)
        {
            this.viewListProvider = viewListProvider;
        }
        public void CreateView(IModule module)
        {
            FrameworkElement view = (FrameworkElement)module.View;
            if (view == null)
            {
                Type viewType = viewListProvider.GetViewType(module.GetType());
                view = (FrameworkElement)Activator.CreateInstance(viewType);
            }
            view.Opacity = 0.0;
            IModuleView viewAsIModuleView = view as IModuleView;
            if (viewAsIModuleView != null)
            {
                viewAsIModuleView.SetViewIsReadyToAppear(false);
            }
            module.SetView(view);
            view.DataContext = module;
        }
        public void ShowView(IModule module)
        {
            FrameworkElement view = (FrameworkElement)module.View;
            IModuleView viewAsIModuleView = view as IModuleView;
            if (viewAsIModuleView != null)
            {
                viewAsIModuleView.BeforeViewDisappear += OnViewBeforeViewDisappear;
                viewAsIModuleView.AfterViewDisappear += OnViewAfterViewDisappear;
                viewAsIModuleView.ViewIsVisibleChanged += OnViewViewIsVisibleChanged;
            }
            view.Opacity = 1.0;
            if (viewAsIModuleView != null)
            {
                viewAsIModuleView.SetViewIsReadyToAppear(true);
            }
        }
        public IModule GetModule(object view)
        {
            FrameworkElement viewAsFrameworkElement = view as FrameworkElement;
            return viewAsFrameworkElement == null ? null : viewAsFrameworkElement.DataContext as IModule;
        }
        void OnViewViewIsVisibleChanged(object sender, EventArgs e)
        {
            FrameworkElement view = (FrameworkElement)sender;
            IModuleView viewAsIModuleView = view as IModuleView;
            IModule module = view.DataContext as IModule;
            if (module != null && viewAsIModuleView != null)
                module.SetIsVisible(viewAsIModuleView.ViewIsVisible);
        }
        void OnViewBeforeViewDisappear(object sender, EventArgs e)
        {
            FrameworkElement view = (FrameworkElement)sender;
            IModule module = view.DataContext as IModule;
            if (module != null)
            {
                foreach (IModule submodule in module.GetSubmodules())
                {
                    if (submodule == null) continue;
                    submodule.RaiseBeforeDisappear();
                }
                module.RaiseBeforeDisappear();
            }
        }
        void OnViewAfterViewDisappear(object sender, EventArgs e)
        {
            FrameworkElement view = (FrameworkElement)sender;
            IModuleView viewAsIModuleView = view as IModuleView;
            IModule module = view.DataContext as IModule;
            if (module != null && module.IsPersistentModule) return;
            if (viewAsIModuleView != null)
            {
                viewAsIModuleView.ViewIsVisibleChanged -= OnViewViewIsVisibleChanged;
                viewAsIModuleView.BeforeViewDisappear -= OnViewBeforeViewDisappear;
                viewAsIModuleView.AfterViewDisappear -= OnViewAfterViewDisappear;
            }
            view.DataContext = null;
            if (module != null)
            {
                foreach (IModule submodule in module.GetSubmodules())
                {
                    if (submodule == null) continue;
                    IModuleView subviewAsIModuleView = submodule.View as IModuleView;
                    if (subviewAsIModuleView != null)
                        subviewAsIModuleView.RaiseAfterViewDisappear();
                }
                module.SetView(null);
                module.Dispose();
            }
            ContentControl cc = view.Parent as ContentControl;
            if (cc != null)
                cc.Content = null;
            ContentPresenter cp = view.Parent as ContentPresenter;
            if (cp != null)
                cp.Content = null;
        }
    }


    [ContentProperty("Content")]
    public class ViewPresenter : Control
    {
        #region Dependency Properties
        public static readonly DependencyProperty ContentProperty;
        static ViewPresenter()
        {
            Type ownerType = typeof(ViewPresenter);
            ContentProperty = DependencyProperty.Register("Content", typeof(object), ownerType, new PropertyMetadata(null, RaiseContentChanged));
        }
        static void RaiseContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ViewPresenter)d).RaiseContentChanged(e.OldValue, e.NewValue);
        }
        #endregion

        //Grid grid; 
        ContentPresenter root;
        /// <summary>
        /// 
        /// </summary>
        public ViewPresenter()
        {
            this.DefaultStyleKey = typeof(ViewPresenter);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        public object Content { get { return GetValue(ContentProperty); } set { SetValue(ContentProperty, value); } }
        protected virtual void SubscribeToViewIsReadyToAppearChanged(object view, EventHandler handler)
        {
            IControlView v = view as IControlView;
            if (v != null)
                v.ViewIsReadyToAppearChanged += handler;
        }
        protected virtual void UnsubscribeFromViewIsReadyToAppearChanged(object view, EventHandler handler)
        {
            IControlView v = view as IControlView;
            if (v != null)
                v.ViewIsReadyToAppearChanged -= handler;
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
        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            BuildVisualTree();
        }
        protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ClearVisualTree();
        }
        void BuildVisualTree()
        {
        }
        void ClearVisualTree()
        {
            if (this.root != null)
                this.root.Content = null;
        }
        void RaiseContentChanged(object oldValue, object newValue)
        {
            IControlView OldValue = oldValue as IControlView, NewValue = newValue as IControlView;
            if (OldValue != null)
            {
                RaiseBeforeViewDisappear(OldValue);
                RaiseAfterViewDisappear(OldValue);
            }

            if (this.root != null)
                this.root.Content = NewValue;

        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.root = (ContentPresenter)GetTemplateChild("Root");
            BuildVisualTree();
        }
    }
}
