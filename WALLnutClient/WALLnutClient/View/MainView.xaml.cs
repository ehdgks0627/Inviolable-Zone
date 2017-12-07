using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WALLnutClient
{
    /// <summary>
    /// MainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainView : BaseUserControls
    {
        public MainView()
        {
            InitializeComponent();
        }

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
                (this.DataContext as MainViewModel).DirPath = dlg.FileName;

                RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                regKey.SetValue("WALLnutFolderPath", dlg.FileName, RegistryValueKind.String);
            }
        }
        #endregion
    }
}
