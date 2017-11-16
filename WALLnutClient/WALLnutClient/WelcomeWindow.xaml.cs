using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;

namespace WALLnutClient
{
    /// <summary>
    /// Interaction logic for BlackWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : MetroWindow
    {
        public WelcomeWindow()
        {
            this.Hide();
            InitializeComponent();
            img_logo.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "logo.png", UriKind.RelativeOrAbsolute));
            RegistryKey reg = Registry.CurrentUser;
            reg = reg.OpenSubKey("SOFTWARE\\WALLnut", true);
            
            if (!Object.ReferenceEquals(reg, null) && 
                !Object.ReferenceEquals(reg.GetValue("access_token"), null) &&
                !reg.GetValue("access_token").ToString().Equals(string.Empty))
            {
                Connection.access_token = reg.GetValue("access_token").ToString();
                nextWindow();
            }
            else
            {
                this.Show();
            }
        }

        private async void btn_start_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> body = new Dictionary<string, string>
            {
               { "api_key", tb_apikey.Text }
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
                    MessageBox.Show(response["err_msg"].ToString());
                    return;
                }
                Connection.access_token = response["access_token"].ToString();

                RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                regKey.SetValue("access_token", Connection.access_token, RegistryValueKind.String);

                nextWindow();
            }
        }

        public void nextWindow()
        {
            this.Hide();
            SelectDiskWindow window = new SelectDiskWindow();
            window.checkWALLnutDriveExist();
        }
    }
}
