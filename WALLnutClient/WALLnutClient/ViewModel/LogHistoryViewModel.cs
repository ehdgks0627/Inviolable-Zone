using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WALLnutClient
{
    public class LogHistoryViewModel : BaseControlViewModel
    {
        public ICommand SearchCommand { get; private set; }


        private String _StartDate = "2017.12.02";
        public String StartDate
        {
            get { return _StartDate; }
            set { SetProperty(ref _StartDate, value); }
        }

        private String _EndDate = "2017.12.02";
        public String EndDate
        {
            get { return _EndDate; }
            set { SetProperty(ref _EndDate, value); }
        }




        public LogHistoryViewModel()
        {
            
        }



        #region override

        protected override void InitData(object parameter)
        {
            base.InitData(parameter);

            Title = "로그 기록";
        }

        protected override void LoadData(object parameter)
        {
            base.LoadData(parameter);
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            SearchCommand = new DelegateCommand<Object>(onSearchLog);
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();
        }

        #endregion //override



        private void onSearchLog(object obj)
        {
        }
    }
}
