using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;


namespace WALLnutClient
{
    [ContentProperty("Modules")]
    public class WindowRoot : Control, ISupportInitialize, IDisposable
    {
        #region Dependency Properties 
        public static readonly DependencyProperty MainModuleProperty;
        #endregion //Dependency Properties

        public IModulesManager ModulesManager { get; private set; }
        public ModuleDescriptionCollection Modules { get; private set; }
        public BaseMainControlViewModel MainModule { get { return (BaseMainControlViewModel)GetValue(MainModuleProperty); } set { SetValue(MainModuleProperty, value); } }


        static WindowRoot()
        {
            Type ownerType = typeof(WindowRoot);
            MainModuleProperty = DependencyProperty.Register("MainModule", typeof(BaseMainControlViewModel), ownerType, new PropertyMetadata(null));
        }
        public WindowRoot()
        {
            DefaultStyleKey = typeof(WindowRoot);

            Modules = new ModuleDescriptionCollection();
            ModulesManager = new ModulesManager(new ViewsManager(Modules), Modules);
        }

        public void Dispose()
        {
            if (ModulesManager != null) ModulesManager = null;
            if (Modules != null) { Modules.Clear(); Modules = null; }
            if (MainModule != null) { MainModule.Dispose(); MainModule = null; }
        }


        public override void BeginInit()
        {
            base.BeginInit();
        }

        public override void EndInit()
        {
            base.EndInit();

            Type mainModuleType = Modules.GetMainModuleType();
            MainModule = mainModuleType == null ? null : (BaseMainControlViewModel)ModulesManager.CreateModule(mainModuleType, null, null);
        }

        public void SetModules(ModuleDescriptionCollection modules, BaseMainControlViewModel mainModule)
        {
            Modules = modules;
            ModulesManager = new ModulesManager(new ViewsManager(Modules), Modules);
            if (mainModule != null)
                MainModule = mainModule;
        }
    }
}
