using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WALLnutClient
{
    public class SettingViewModel : BaseControlViewModel
    {
        public ICommand LicenseContractCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }


        private Boolean _IsStartWindow = false;
        /// <summary>
        /// 윈도우 시작 시 실행
        /// </summary>
        public Boolean IsStartWindow
        {
            get { return _IsStartWindow; }
            set { SetProperty(ref _IsStartWindow, value); }
        }

        private int _LogSavePeriod = 0;
        /// <summary>
        /// 로그 저장 기간
        /// </summary>
        public int LogSavePeriod
        {
            get { return _LogSavePeriod; }
            set { SetProperty(ref _LogSavePeriod, value); }
        }



        public SettingViewModel()
        {

        }


        #region override

        protected override void InitData(object parameter)
        {
            base.InitData(parameter);

            Title = "환경설정";
            LogSavePeriod = 30;
        }

        protected override void LoadData(object parameter)
        {
            base.LoadData(parameter);
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            LicenseContractCommand = new DelegateCommand<Object>(onLicenseContract);
            LogoutCommand = new DelegateCommand<Object>(onLogout);
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();
        }

        #endregion //override



        private void onLicenseContract(object obj)
        {
        }

        private void onLogout(object obj)
        {
        }
    }
}
