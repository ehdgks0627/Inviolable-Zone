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
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;

namespace WALLnutClient
{
    public partial class MainWindow : MetroWindow
    {
        FileSystemWatcher fs = null;
        List<string> BlackListExtensions = new List<string>();
        DiskManager manager = null;
        DiskInfo info = null;
        FileExplorer explorer = null;
        bool isFileExporerActivate = false;
        bool realTimeSync = false;

        /* 
            TODO List
            디스크 용량 초과?
        */

        #region [Funciton] test function
        public void TestCase(string drivename)
        {
            Debug.Assert(DiskManager.FormatDisk(new DiskInfo { DeviceID = drivename }).Equals(true));
            manager = new DiskManager(drivename);
            Debug.Assert(manager.isActive.Equals(true));
            Debug.Assert(manager.Path2Offset(@"a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.Path2Offset(@"\a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.Path2Offset(@"\") == 2);

            /*for(ulong i=3; i<40000; i++)
            {
                Debug.Assert(manager.AvailableBlock(DiskManager.BLOCKTYPE.DATA) == i);
                Console.WriteLine(i);
            }*/
            byte[] data = new byte[200];
            byte[] read_data;
            Random r = new Random((int)(DateTime.Now.ToFileTimeUtc()));
            FileStream fs = new FileStream(@"test.txt", FileMode.Create);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(r.Next(1, 255));
                fs.WriteByte(data[i]);
            }
            fs.Close();

            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", @"test.txt").Equals(true));
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.WriteFile(@"\test\a", @"test.txt").Equals(false));
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.WriteFolder(@"\test\a").Equals(false));
            Debug.Assert(manager.WriteFile(@"\test\a\wow", @"test.txt").Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", @"test.txt").Equals(true));
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(true));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }

            Debug.Assert(manager.WriteFile(@"\테스트", @"test.txt").Equals(true));
            Debug.Assert(manager.ReadFile(@"\테스트", out read_data).Equals(true));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            //Debug.Assert(manager.DeleteFile(@"\테스트").Equals(true));

            Debug.Assert(manager.DeleteFile(@"\test").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(false));
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", @"test.txt").Equals(true));
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(true));
            Debug.Assert(manager.Path2Offset(@"\test") == 3);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            Debug.Assert(manager.DeleteFile(@"test").Equals(false));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(false));

            Debug.Assert(manager.WriteFile(@"\asdf\test", @"test.txt").Equals(false));
            Debug.Assert(manager.WriteFolder(@"asdf\asdf").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\\").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\\\").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\\a").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\asdf\asdf").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\asdf").Equals(true));
            Debug.Assert(manager.WriteFolder(@"\asdf").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\asdf").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\asdf").Equals(false));
            Debug.Assert(manager.WriteFolder(@"\asdf").Equals(true));
            Debug.Assert(manager.WriteFile(@"\asdf\test", @"test.txt").Equals(true));
            Debug.Assert(manager.WriteFolder(@"\asdf\test").Equals(false));
            Debug.Assert(manager.WriteFile(@"\asdf", @"test.txt").Equals(false));
            Debug.Assert(manager.WriteFile(@"\asdf\\", @"test.txt").Equals(false));
            Debug.Assert(manager.WriteFile(@"asdf\test", @"test.txt").Equals(false));
            Debug.Assert(manager.ReadFile(@"\asdf\test", out read_data));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            Debug.Assert(manager.DeleteFile(@"\asdf\test").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\asdf\test").Equals(false));

            Debug.Assert(manager.GetAvailableBit(0x00) == 0);
            Debug.Assert(manager.GetAvailableBit(0x01) == 1);
            Debug.Assert(manager.GetAvailableBit(0x03) == 2);
            Debug.Assert(manager.GetAvailableBit(0x07) == 3);
            Debug.Assert(manager.GetAvailableBit(0x80) == 0);
            Debug.Assert(manager.GetAvailableBit(0xFF) == 0xFF);

            Debug.Assert(manager.SetBitMapBlock(1).Equals(false));
            Debug.Assert(manager.SetBitMapBlock(2).Equals(false));

            Debug.Assert(manager.SetBitMapBlock(4).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(35).Equals(true));

            Debug.Assert(manager.SetBitMapBlock(4076).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4076).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076).Equals(true));

            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 8 - 1).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076 * 8 - 1).Equals(true));

            Debug.Assert(manager.SetBitMapBlock(4076 * 19).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076 * 19).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 19).Equals(true));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 19).Equals(false));
            Debug.Assert(manager.SetBitMapBlock(4076 * 19).Equals(true));

            Debug.Assert(manager.SetBitMapBlock(4076 * 100).Equals(true));
            Debug.Assert(manager.SetBitMapBlock(4076 * 100).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100).Equals(true));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100).Equals(false));
            Debug.Assert(manager.SetBitMapBlock(4076 * 100).Equals(true));

            //할당되지 않은 블록에 대한 UnSet
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 100 + 1).Equals(false));
            Debug.Assert(manager.UnSetBitMapBlock(4076 * 200).Equals(false));
        }
        #endregion

        public MainWindow(DiskInfo _info)
        {
            InitializeComponent();
            info = _info;
            img_onoff.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "onoff.png", UriKind.RelativeOrAbsolute));
            img_filesystem.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "filesystem.png", UriKind.RelativeOrAbsolute));
            img_setting.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "setting.png", UriKind.RelativeOrAbsolute));
            //tb_path.Text = System.AppDomain.CurrentDomain.BaseDirectory;
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

            //TestCase(@"\\.\PhysicalDrive1");
            manager = new DiskManager(info.DeviceID);
            Double usage = (manager.getUsage() / info.Size);
            pb_diskusage.Value = usage;
        }

        private void btn_setting_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region [Function] WALLnut 탐색기 창이 닫을때 호출됩니다
        public void OnCloseFileExplorer()
        {
            isFileExporerActivate = false;
        }
        #endregion

        #region [Function] Mime 타입의 파일 형식을 반환합니다
        public string GetMime(string path)
        {
            return " ";// MimeGuesser.GuessMimeType(path);
        }
        #endregion

        #region [Function] 해당 경로가 폴더인지 확인합니다
        public bool isFolder(string path)
        {
            FileAttributes attr = File.GetAttributes(path);

            //detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region [Function] WALLnut 탐색기 창을 엽니다
        private void btn_filesystem_Click(object sender, RoutedEventArgs e)
        {
            if (!isFileExporerActivate)
            {
                isFileExporerActivate = true;
                explorer = new FileExplorer(manager.root);
                explorer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                explorer.Closed += (object child_sender, EventArgs child_e) => OnCloseFileExplorer();
                explorer.Show();
            }
            else
            {
                explorer.Focus();
            }
        }
        #endregion

        #region [Function] FileSystemSatcher 이벤트 핸들러
        protected void event_CreateFile(object fscreated, FileSystemEventArgs Eventocc)
        {
            bool folder = false;
            string mime = string.Empty;
            if (realTimeSync)
            {
                folder = isFolder(Eventocc.FullPath);
                mime = GetMime(Eventocc.FullPath);
                try
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (folder)
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "Folder Created - " + Eventocc.Name);
                        }
                        else
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Created - " + Eventocc.Name);
                        }
                    }));
                    if (folder)
                    {
                        manager.WriteFolder(@"\" + Eventocc.Name);
                    }
                    else
                    {
                        manager.WriteFile(@"\" + Eventocc.Name, Eventocc.FullPath);
                    }
                }
                catch
                {

                }
            }
        }

        protected void event_ChangeFile(object fschanged, FileSystemEventArgs changeEvent)
        {
            bool folder = false;
            string mime = string.Empty;
            if (realTimeSync)
            {
                folder = isFolder(changeEvent.FullPath);
                mime = GetMime(changeEvent.FullPath);
                try
                {
                    fs.EnableRaisingEvents = false;
                    if (folder)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "Folder Changed - " + changeEvent.Name);
                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Changed - " + changeEvent.Name);
                        }));
                    }
                }
                catch
                {
                }
                finally
                {
                    fs.EnableRaisingEvents = true;
                }
            }
        }

        protected void event_RenameFile(object fschanged, RenamedEventArgs changeEvent)
        {
            bool folder = false;
            string mime = string.Empty;
            if (realTimeSync)
            {
                try
                {
                    folder = isFolder(changeEvent.FullPath);
                    mime = GetMime(changeEvent.FullPath);
                    if (folder)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "Folder Renamed - " + changeEvent.Name + ", oldname : " + changeEvent.OldName);
                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "File Renamed - " + changeEvent.Name + ", oldname : " + changeEvent.OldName);
                        }));
                    }
                    manager.Rename(@"\" + changeEvent.OldName, @"\" + changeEvent.Name);
                }
                catch
                {
                }
            }
        }

        protected void event_DeleteFile(object fschanged, FileSystemEventArgs changeEvent)
        {
            if (realTimeSync)
            {
                try
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        lv_log.Items.Add("[" + DateTime.Now.ToShortTimeString() + "]" + "Deleted - " + changeEvent.Name);
                    }));
                    manager.DeleteFile(@"\" + changeEvent.Name);
                }
                catch
                {
                }
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

        #region [Function] 실시간 파일 동기화를 키고 끕니다
        private void btn_onoff_Click(object sender, RoutedEventArgs e)
        {
            if (!realTimeSync)
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
                    fs.InternalBufferSize = 1024 * 64;
                    fs.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastAccess | NotifyFilters.LastWrite;

                    realTimeSync = true;
                    tb_path.IsEnabled = false;
                    MessageBox.Show("동기화 시작!");
                }
                catch
                {
                    MessageBox.Show("올바른 경로가 아님...");
                }
            }
            else
            {
                fs.Dispose();
                tb_path.IsEnabled = true;
                realTimeSync = false;
                MessageBox.Show("동기화 해제!");
            }
        }
        #endregion
    }
}