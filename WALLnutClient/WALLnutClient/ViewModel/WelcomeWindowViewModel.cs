using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WALLnutClient
{
    public class WelcomeWindowViewModel : BindableBase
    {
        public ICommand StartCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }


        private String _ApiKey = String.Empty;
        public String ApiKey
        {
            get { return _ApiKey; }
            set { SetProperty(ref _ApiKey, value); }
        }


        public WelcomeWindowViewModel()
        {
            StartCommand = new DelegateCommand<Object>(onStart);
            CloseCommand = new DelegateCommand<Object>(onClose);
            Welcome();
        }

        private void Welcome()
        {
            RegistryKey reg = Registry.CurrentUser;
            reg = reg.OpenSubKey("SOFTWARE\\WALLnut", true);

            if (!Object.ReferenceEquals(reg, null) &&
                !Object.ReferenceEquals(reg.GetValue("access_token"), null) &&
                !reg.GetValue("access_token").ToString().Equals(string.Empty))
            {
                Connection.accessToken = reg.GetValue("access_token").ToString();
                nextWindow();
            }
            else
            {
                App.Current.MainWindow.Show();
            }
        }

        private async void onStart(object obj)
        {
            Dictionary<string, string> body = new Dictionary<string, string>
            {
               { "api_key", ApiKey }
            };
            Task<string> result_task = Connection.PostRequest("/v1/user/join/", body);
            string result = await result_task;
            if (result.Equals(""))
            {
                return;
            }
            else
            {
                JObject response = JObject.Parse(result);

                if (!Object.ReferenceEquals(response["err_msg"], null))
                {
                    MessageBox.Show(response["err_msg"].ToString(), "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Connection.accessToken = response["access_token"].ToString();

                RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                regKey.SetValue("access_token", Connection.accessToken, RegistryValueKind.String);

                nextWindow();
            }
        }

        private void onClose(object obj)
        {
            App.Current.Shutdown();
        }


        private void nextWindow()
        {
            WelcomeWindow welcome = App.Current.Windows.OfType<WelcomeWindow>().FirstOrDefault();
            SelectDiskWindow window = new SelectDiskWindow();
            if (!(window.DataContext as SelectDiskWindowViewModel).checkWALLnutDriveExist())
            {
                App.Current.MainWindow = window;
                window.Show();
            }
            if (welcome != null)
                welcome.Close();
        }
    }
}
