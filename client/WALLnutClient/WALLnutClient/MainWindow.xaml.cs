using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace WALLnutClient
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher fs;
        List<string> BlackListExtensions = new List<string>();
        public MainWindow()
        {
            InitializeComponent();

            tb_path.Text = System.AppDomain.CurrentDomain.BaseDirectory;

            #region [Code] 블랙리스트 파일을 읽어와서 리스트에 저장
            using (StreamReader sr = new StreamReader(@"..\..\ext.data"))
            {
                while(true)
                {
                    String line = sr.ReadLine();
                    if ( line == null)
                    {
                        break;
                    }
                    BlackListExtensions.Add(line);
                }
            }
            #endregion
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
