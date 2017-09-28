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

namespace WALLnutClient
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher fs = null;
        List<string> BlackListExtensions = new List<string>();

        internal partial class DeviceIO
        {

            #region Constants used in unmanaged functions

            public const uint FILE_SHARE_READ = 0x00000001;
            public const uint FILE_SHARE_WRITE = 0x00000002;
            public const uint FILE_SHARE_DELETE = 0x00000004;
            public const uint OPEN_EXISTING = 3;

            public const uint GENERIC_READ = (0x80000000);
            public const uint GENERIC_WRITE = (0x40000000);

            public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
            public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
            public const uint FILE_READ_ATTRIBUTES = (0x0080);
            public const uint FILE_WRITE_ATTRIBUTES = 0x0100;
            public const uint ERROR_INSUFFICIENT_BUFFER = 122;

            #endregion

            #region Unamanged function declarations
           
            [DllImport("kernel32.dll", SetLastError = true)]
            public static unsafe extern SafeFileHandle CreateFile(
                string FileName,
                uint DesiredAccess,
                uint ShareMode,
                IntPtr SecurityAttributes,
                uint CreationDisposition,
                uint FlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(SafeFileHandle hHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                IntPtr lpInBuffer,
                uint nInBufferSize,
                [Out] IntPtr lpOutBuffer,
                uint nOutBufferSize,
                ref uint lpBytesReturned,
                IntPtr lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern unsafe bool WriteFile(
                SafeFileHandle hFile,
                byte* pBuffer,
                uint NumberOfBytesToWrite,
                uint* pNumberOfBytesWritten,
                IntPtr Overlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern unsafe bool ReadFile(
                SafeFileHandle hFile,
                byte* pBuffer,
                uint NumberOfBytesToRead,
                uint* pNumberOfBytesRead,
                IntPtr Overlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetFilePointerEx(
                SafeFileHandle hFile,
                ulong liDistanceToMove,
                out ulong lpNewFilePointer,
                uint dwMoveMethod);

            [DllImport("kernel32.dll")]
            public static extern bool FlushFileBuffers(
                SafeFileHandle hFile);

            #endregion

        }

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
            
            string a = "";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume");
            ManagementObjectCollection collection = searcher.Get();
            
            foreach (ManagementObject item in collection)
            {
                System.Console.WriteLine(
                    "Name: {0}, Device ID: {1}",
                     item["Name"], item["DeviceID"]);
                a = item["DeviceID"].ToString();
            }
            
            unsafe
            {
                SafeFileHandle h = DeviceIO.CreateFile(a, DeviceIO.GENERIC_READ, DeviceIO.FILE_SHARE_READ | DeviceIO.FILE_SHARE_WRITE, IntPtr.Zero, DeviceIO.OPEN_EXISTING, 0, IntPtr.Zero);
                byte[] buf = new byte[80];
                uint[] read = new uint[4];
                fixed(byte* buffer = &buf[0])
                {
                    fixed (uint* readed = &read[0])
                    {
                        DeviceIO.ReadFile(h, buffer, 80, readed, IntPtr.Zero);
                        DeviceIO.CloseHandle(h);
                    }
                }
            }
            
           
            try
            {
                using (StreamReader sr = new StreamReader(a))
                {
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }

        }

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
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception err)
            {
                MessageBox.Show("올바른 경로가 아님...");
            }
        }
        #endregion
    }
}
