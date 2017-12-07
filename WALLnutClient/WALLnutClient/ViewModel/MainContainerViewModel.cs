using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WALLnutClient
{
    public class MainContainerViewModel : BaseMainControlViewModel
    {
        public DiskInfo Info { get; set; }


        public ICommand ShowMainViewCommand { get; private set; }
        public ICommand ShowFileExplorerCommand { get; private set; }
        public ICommand ShowSettingCommand { get; private set; }
        public ICommand ShowLogHistoryCommand { get; private set; }


        private ObservableCollection<DiskLog> _lvLog = new ObservableCollection<DiskLog>();
        public ObservableCollection<DiskLog> lvLog
        {
            get { return _lvLog; }
            set { SetProperty(ref _lvLog, value); }
        }

        private String _ProgramVer = String.Empty;
        public String ProgramVer
        {
            get { return _ProgramVer; }
            set { SetProperty(ref _ProgramVer, value); }
        }




        public MainContainerViewModel()
            : base()
        {
            
        }



        protected override ICommand CreateShowModuleCommand<T>(Object param)
        {
            return new ExtendedActionCommand(p => ShowModule<T>(p), this, "CurrentModuleType", x => CurrentModuleType != typeof(T), param);
        }

        protected override void LoadData(object parameter)
        {
            base.LoadData(parameter);
        }

        protected override void InitData(object parameter)
        {
            base.InitData(parameter);

            ProgramVer = String.Format("버전 : {0}", DateTime.Now.ToShortDateString());
            if (Info != null)
            {
                WallrutInfo.Info.Initial(Info);
                if (WallrutInfo.Info.FileWatcher != null)
                    WallrutInfo.Info.FileWatcher.addDiskLog += FileWatcher_addDiskLog;
            }
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            ShowMainViewCommand = CreateShowModuleCommand<MainViewModel>(Info);
            ShowFileExplorerCommand = CreateShowModuleCommand<FileExplorerViewModel>(null);
            ShowSettingCommand = CreateShowModuleCommand<SettingViewModel>(null);
            ShowLogHistoryCommand = CreateShowModuleCommand<LogHistoryViewModel>(null);
            CloseCommand = new DelegateCommand<Object>(onClose);
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();

            WallrutInfo.Info.FileWatcher.addDiskLog -= FileWatcher_addDiskLog;

            ShowMainViewCommand = null;
            ShowFileExplorerCommand = null;
            ShowSettingCommand = null;
            ShowLogHistoryCommand = null;
        }



        private void FileWatcher_addDiskLog(DiskLog diskLog)
        {
            lvLog.Insert(0, diskLog);
        }
        private void onClose(object obj)
        {
            if (WallrutInfo.Info != null)
                WallrutInfo.Info.Dispose();
            App.Current.Shutdown();
        }
    }
}
