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

namespace WALLnutClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string watchingFolder = @"C:\";
        FileSystemWatcher fs;
        public MainWindow()
        {
            InitializeComponent();

            fs = new FileSystemWatcher(watchingFolder, "*.*");

            fs.Created += new FileSystemEventHandler(event_CreateFile);
            fs.Changed += new FileSystemEventHandler(event_ChangeFile);
            fs.Renamed += new RenamedEventHandler(event_RenameFile);
            fs.Deleted += new FileSystemEventHandler(event_DeleteFile);

            fs.EnableRaisingEvents = true;
            fs.IncludeSubdirectories = true;
        }

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
    }
}
