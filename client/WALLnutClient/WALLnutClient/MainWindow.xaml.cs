using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO.Pipes;

namespace WALLnutClient
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher fs = null;
        List<string> BlackListExtensions = new List<string>();
        DiskManager manager = null;

        public MainWindow()
        {
            InitializeComponent();

            tb_path.Text = System.AppDomain.CurrentDomain.BaseDirectory;
            #region [Code] 블랙리스트 파일을 읽어와서 리스트에 저장
            using (StreamReader sr = new StreamReader(@"..\..\ext.data"))
            {
                while (true)
                {
                    String line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    BlackListExtensions.Add(line);
                }
            }
            #endregion

            UpdateDriveList();
            /*
            manager = new DiskManager("\\\\.\\PhysicalDrive1");
            if(manager.isActive)
            {
                manager.Path2Offset("\\a");
            }*/
        }

        #region [Function] Drive 목록 업데이트
        public void UpdateDriveList()
        {
            List<DiskInfo> list = GetDriveList();
            cb_disk.Items.Clear();
            foreach (DiskInfo info in list)
            {
                cb_disk.Items.Add(info);
            }
            cb_disk.SelectedIndex = 0;
        }
        #endregion

        #region [Function] FileSystemSatcher 이벤트 핸들러
        protected void event_CreateFile(object fscreated, FileSystemEventArgs Eventocc)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Created - " + Eventocc.Name);
                }));
            }
            catch
            {
                
            }
        }

        protected void event_ChangeFile(object fschanged, FileSystemEventArgs changeEvent)
        {
            try
            {
                fs.EnableRaisingEvents = false;
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Changed - " + changeEvent.Name);
                }));
            }
            catch
            {
            }
            finally
            {
                fs.EnableRaisingEvents = true;
            }

        }

        protected void event_RenameFile(object fschanged, RenamedEventArgs changeEvent)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Renamed - " + changeEvent.Name + ", oldname : " + changeEvent.OldName);
                }));
            }
            catch
            {
            }
        }

        protected void event_DeleteFile(object fschanged, FileSystemEventArgs changeEvent)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Deleted - " + changeEvent.Name);
                }));
            }
            catch
            {
            }
        }
        #endregion

        #region [Function] 경로 설정 텍스트 박스 마우스 버튼 핸들러
        private void tb_path_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "Select Directory";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tb_path.Text = dlg.FileName;
            }
        }
        #endregion

        #region [Function] 동기화 버튼 핸들러
        private void btn_sync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fs = new FileSystemWatcher(tb_path.Text, "*.*");

                fs.Created += new FileSystemEventHandler(event_CreateFile);
                fs.Changed += new FileSystemEventHandler(event_ChangeFile);
                fs.Renamed += new RenamedEventHandler(event_RenameFile);
                fs.Deleted += new FileSystemEventHandler(event_DeleteFile);

                fs.EnableRaisingEvents = true;
                fs.IncludeSubdirectories = true;

                MessageBox.Show("동기화 시작!");
            }
            catch
            {
                MessageBox.Show("올바른 경로가 아님...");
            }
        }
        #endregion

        #region [Function] PhysicalDrive의 목록을 반환
        public List<DiskInfo> GetDriveList()
        {
            List<DiskInfo> result = new List<DiskInfo>();
            Process WmicProcess = new Process();
            WmicProcess.StartInfo.FileName = "wmic.exe";
            WmicProcess.StartInfo.UseShellExecute = false;
            WmicProcess.StartInfo.Arguments = "diskdrive list brief / format:list";
            WmicProcess.StartInfo.RedirectStandardOutput = true;
            WmicProcess.StartInfo.CreateNoWindow = true;
            WmicProcess.Start();

            string[] lines = WmicProcess.StandardOutput.ReadToEnd().Split(new[] { "\r\r\n\r\r\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                DiskInfo diskinfo = new DiskInfo();
                string[] infos = line.Split(new[] { "\r\r\n" }, StringSplitOptions.None);
                foreach (string info in infos)
                {
                    try
                    {
                        string[] t = info.Split('=');
                        if (t[0] == "Caption")
                        {
                            diskinfo.Caption = t[1];
                        }
                        else if (t[0] == "DeviceID")
                        {
                            diskinfo.DeviceID = t[1];
                        }
                        else if (t[0] == "Model")
                        {
                            diskinfo.Model = t[1];
                        }
                        else if (t[0] == "Partitions")
                        {
                            diskinfo.Partitions = Convert.ToUInt64(t[1]);
                        }
                        else if (t[0] == "Size")
                        {
                            diskinfo.Size = Convert.ToUInt64(t[1]);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                result.Add(diskinfo);
            }
            WmicProcess.WaitForExit();
            WmicProcess.Close();
            return result;
        }
        #endregion

        private void btn_format_Click(object sender, RoutedEventArgs e)
        {
            if (cb_disk.SelectedIndex == -1)
            {
                MessageBox.Show("포맷할 디스크를 선택해주세요!", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBoxResult.OK == MessageBox.Show(
                "정말로 포맷하시겠습니까? 디스크 내 모든 데이터가 초기화됩니다!",
                "매우 주의",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning))
            {
                if (DiskManager.FormatDisk((DiskInfo)cb_disk.SelectedItem))
                {
                    MessageBox.Show("포맷 성공", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
