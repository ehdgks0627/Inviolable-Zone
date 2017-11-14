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
using System.Management;
using Microsoft.Win32;

namespace WALLnutClient
{
    /// <summary>
    /// Interaction logic for BlackWindow.xaml
    /// </summary>
    public partial class SelectDiskWindow : MetroWindow
    {
        public bool IsClosed { get; private set; }

        List<DiskInfo> infos = new List<DiskInfo>();
        public SelectDiskWindow()
        {
            this.Hide();
            InitializeComponent();
            UpdateDriveList();
            img_refresh.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "refresh.png", UriKind.RelativeOrAbsolute));
        }

        #region [Function] 버튼 버튼 핸들러
        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            DiskInfo info = null;
            if (cb_disk.SelectedIndex.Equals(-1))
            {
                MessageBox.Show("진행할 디스크를 선택해주세요!", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            info = (DiskInfo)cb_disk.Items[cb_disk.SelectedIndex];
            if (info.isWALLNutDevice)
            {
                MainWindow window = new MainWindow(info);
                window.Show();
                this.Close();
            }
            else
            {
                if (MessageBoxResult.OK.Equals(MessageBox.Show(
                "정말로 포맷하시겠습니까? 디스크 내 모든 데이터가 초기화됩니다!",
                "주의",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning)))
                {
                    Int64 uuid = DiskManager.FormatDisk((DiskInfo)cb_disk.SelectedItem);
                    if (!uuid.Equals(0))
                    {
                        RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                        regKey.SetValue("WALLnutDriveUUID", uuid, RegistryValueKind.String);
                        MessageBox.Show("포맷 성공", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                        nextWindow(info);
                    }
                }
            }
        }
        #endregion

        #region [Function] Drive 목록 업데이트
        public void UpdateDriveList()
        {
            infos = DiskInfo.GetDriveList();
            cb_disk.Items.Clear();
            foreach (DiskInfo info in infos)
            {
                cb_disk.Items.Add(info);
            }
            cb_disk.SelectedIndex = 0;
        }
        #endregion

        private void cb_disk_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cb_disk.SelectedIndex.Equals(-1))
            {
                DiskInfo info = (DiskInfo)cb_disk.Items[cb_disk.SelectedIndex];
                if (!info.isWALLNutDevice)
                {
                    btn_ok.Content = "Format";
                }
                else
                {
                    btn_ok.Content = "Continue";
                }
            }
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateDriveList();
        }

        public void nextWindow(DiskInfo info)
        {
            MainWindow window = new MainWindow(info);
            window.Show();
            this.Close();
        }

        public void checkWALLnutDriveExist()
        {
            RegistryKey reg = Registry.CurrentUser;
            reg = reg.OpenSubKey("SOFTWARE\\WALLnut", true);

            if (!Object.ReferenceEquals(reg, null) &&
                !Object.ReferenceEquals(reg.GetValue("WALLnutDriveUUID"), null) &&
                !reg.GetValue("WALLnutDriveUUID").ToString().Equals(string.Empty))
            {
                Int64 uuid = Int64.Parse((reg.GetValue("WALLnutDriveUUID").ToString()));
                if (!Object.ReferenceEquals(infos.Find(x => x.uuid.Equals(uuid)), null))
                {
                    nextWindow(infos.Find(x => x.uuid.Equals(uuid)));
                }
            }
            if (!this.IsClosed)
            {
                this.Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }
    }
}
