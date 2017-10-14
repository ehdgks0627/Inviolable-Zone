using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO.Pipes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace WALLnutClient
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher fs = null;
        List<string> BlackListExtensions = new List<string>();
        DiskManager manager = null;
        /* 
            TODO List
            맨처음 동작 할때 파일리스트 읽어와서 백업
            디스크 용량 초과?
            폴더 삭제 처리해줘야 할듯 어떻게할까? 그냥 파일마다 일일히 삭제 때리면 될 듯 한데
        */

        #region [Funciton] test function
        public void TestCase(string drivename)
        {
            Debug.Assert(DiskManager.FormatDisk(new DiskInfo { DeviceID = drivename }) == true);
            manager = new DiskManager(drivename);
            Debug.Assert(manager.isActive == true);
            Debug.Assert(manager.Path2Offset(@"a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.Path2Offset(@"\a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.Path2Offset(@"\") == 2);

            /*for(ulong i=3; i<40000; i++)
            {
                Debug.Assert(manager.AvailableBlock(DiskManager.BLOCKTYPE.DATA) == i);
                Console.WriteLine(i);
            }*/
            byte[] data = new byte[50000];
            byte[] read_data;
            Random r = new Random((int)(DateTime.Now.ToFileTimeUtc()));
            FileStream fs = new FileStream(@"C:\WALLnut\test.txt", FileMode.Create);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(r.Next(1, 255));
                fs.WriteByte(data[i]);
            }
            fs.Close();

            Debug.Assert(manager.ReadFile(@"\test", out read_data) == false);
            Debug.Assert(manager.WriteFile(@"\test", @"C:\WALLnut\test.txt") == true);
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.WriteFile(@"\test\a", @"C:\WALLnut\test.txt") == false);
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.WriteFile(@"\test", @"C:\WALLnut\test.txt") == true);
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.ReadFile(@"\test", out read_data) == true);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }

            Debug.Assert(manager.WriteFile(@"\테스트", @"C:\WALLnut\test.txt") == true);
            Debug.Assert(manager.ReadFile(@"\테스트", out read_data) == true);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            //Debug.Assert(manager.DeleteFile(@"\테스트") == true);

            Debug.Assert(manager.DeleteFile(@"\test") == true);
            Debug.Assert(manager.DeleteFile(@"\test") == false);
            Debug.Assert(manager.ReadFile(@"\test", out read_data) == false);
            Debug.Assert(manager.WriteFile(@"\test", @"C:\WALLnut\test.txt") == true);
            Debug.Assert(manager.ReadFile(@"\test", out read_data) == true);
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            Debug.Assert(manager.DeleteFile(@"test") == false);
            Debug.Assert(manager.DeleteFile(@"\test") == true);
            Debug.Assert(manager.DeleteFile(@"\test") == false);

            Debug.Assert(manager.WriteFile(@"\asdf\test", @"C:\WALLnut\test.txt") == false);
            Debug.Assert(manager.WriteFolder(@"asdf\asdf") == false);
            Debug.Assert(manager.WriteFolder(@"\") == false);
            Debug.Assert(manager.WriteFolder(@"\\") == false);
            Debug.Assert(manager.WriteFolder(@"\\a") == false);
            Debug.Assert(manager.WriteFolder(@"\asdf\asdf") == false);
            Debug.Assert(manager.WriteFolder(@"\asdf") == true);
            Debug.Assert(manager.WriteFolder(@"\asdf") == true);
            Debug.Assert(manager.DeleteFile(@"\asdf") == true);
            Debug.Assert(manager.DeleteFile(@"\asdf") == false);
            Debug.Assert(manager.WriteFolder(@"\asdf") == true);
            Debug.Assert(manager.WriteFile(@"\asdf\test", @"C:\WALLnut\test.txt") == true);
            Debug.Assert(manager.WriteFolder(@"\asdf\test") == false);
            Debug.Assert(manager.WriteFile(@"\asdf", @"C:\WALLnut\test.txt") == false);
            Debug.Assert(manager.WriteFile(@"\asdf\\", @"C:\WALLnut\test.txt") == false);
            Debug.Assert(manager.WriteFile(@"asdf\test", @"C:\WALLnut\test.txt") == false);
            Debug.Assert(manager.ReadFile(@"\asdf\test", out read_data));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            Debug.Assert(manager.DeleteFile(@"\asdf\test") == true);
            Debug.Assert(manager.DeleteFile(@"\asdf\test") == false);

            Debug.Assert(manager.GetAvailableBit(0x00) == 0);
            Debug.Assert(manager.GetAvailableBit(0x01) == 1);
            Debug.Assert(manager.GetAvailableBit(0x03) == 2);
            Debug.Assert(manager.GetAvailableBit(0x07) == 3);
            Debug.Assert(manager.GetAvailableBit(0x80) == 0);
            Debug.Assert(manager.GetAvailableBit(0xFF) == 0xFF);

            Debug.Assert(manager.SetBitMapBlock(1) == false);
            Debug.Assert(manager.SetBitMapBlock(2) == false);

            Debug.Assert(manager.SetBitMapBlock(4) == true);
            Debug.Assert(manager.SetBitMapBlock(4) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4) == true);
            Debug.Assert(manager.SetBitMapBlock(35) == true);

            Debug.Assert(manager.SetBitMapBlock(4076) == true);
            Debug.Assert(manager.SetBitMapBlock(4076) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4076) == true);
            Debug.Assert(manager.SetBitMapBlock(4076) == true);

            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1) == true);
            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 8 - 1) == true);
            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1) == true);
            
            Debug.Assert(manager.SetBitMapBlock(4076 * 19) == true);
            Debug.Assert(manager.SetBitMapBlock(4076 * 19) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 19) == true);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 19) == false);
            Debug.Assert(manager.SetBitMapBlock(4076 * 19) == true);
            
            Debug.Assert(manager.SetBitMapBlock(4076 * 100) == true);
            Debug.Assert(manager.SetBitMapBlock(4076 * 100) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100) == true);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100) == false);
            Debug.Assert(manager.SetBitMapBlock(4076 * 100) == true);

            //할당되지 않은 블록에 대한 UnSet
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100 + 1) == false);
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 200) == false);
        }
        #endregion

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
            //TestCase(@"\\.\PhysicalDrive1");
            manager = new DiskManager(@"\\.\PhysicalDrive1");
        }

        #region [Function] Drive 목록 업데이트
        public void UpdateDriveList()
        {
            List<DiskInfo> list = DiskInfo.GetDriveList();
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

        #region [Function] 포맷 버튼 핸들러
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
                    UpdateDriveList();
                }
            }
        }
        #endregion
    }
}
