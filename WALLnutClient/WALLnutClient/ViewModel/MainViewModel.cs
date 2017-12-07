using HeyRed.Mime;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WALLnutClient
{
    /*
    TODO
    파일 복구 기능
    save log and load
    그래프 추가하기

    디스크 용량 초과?
    시작 아이콘 등록
    환경 설정에 시작 프로그램에 추가(레지스트리)
    USB Eject Event 감지       
     */

    public class DiskUsage
    {
        public string Category { get; set; }
        public Double Usage { get; set; }
    }


    public class MainViewModel : BaseControlViewModel
    {
        List<string> blackListExtensions = new List<string>();

        #region Binding Property
        private ObservableCollection<DiskUsage> _DiskUsage = new ObservableCollection<DiskUsage>();
        public ObservableCollection<DiskUsage> DiskUsage
        {
            get { return _DiskUsage; }
            set { SetProperty(ref _DiskUsage, value); }
        }

        private String _DirPath = @"C:\WALLnut";
        public String DirPath
        {
            get { return _DirPath; }
            set { SetProperty(ref _DirPath, value); }
        }

        private Double _ProgressMax = 100;
        public Double ProgressMax
        {
            get { return _ProgressMax; }
            set { SetProperty(ref _ProgressMax, value); }
        }

        private Double _ProgressValue = 0;
        public Double ProgressValue
        {
            get { return _ProgressValue; }
            set { SetProperty(ref _ProgressValue, value); }
        }

        private bool _PathEnabled = true;
        public bool PathEnabled
        {
            get { return _PathEnabled; }
            set { SetProperty(ref _PathEnabled, value); }
        }

        private String _LastSyncTime = String.Empty;
        public String LastSyncTime
        {
            get { return _LastSyncTime; }
            set { SetProperty(ref _LastSyncTime, value); }
        }
        #endregion //
        
        public ICommand OnOffCommand { get; private set; }
        public ICommand FileSystemCommand { get; private set; }
        public ICommand SettingCommand { get; private set; }
        public ICommand LogCommand { get; private set; }

        public MainViewModel()
        {
            
        }

        protected override void LoadData(object parameter)
        {
            base.LoadData(parameter);
        }

        protected override void InitData(object parameter)
        {
            base.InitData(parameter);

            try
            {
                #region [Code] 블랙리스트 파일을 읽어와서 리스트에 저장
                string blackListPath = @"C:\WALLnut\ext.data";
                blackListPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\ext.data";
                if (File.Exists(blackListPath))
                {
                    using (StreamReader sr = new StreamReader(blackListPath))
                    {
                        while (true)
                        {
                            String line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            blackListExtensions.Add(line);
                        }
                    }
                }
                #endregion

                DiskUsage.Add(new DiskUsage() { Category = "사용량", Usage = 30 });
                DiskUsage.Add(new DiskUsage() { Category = "빈공간", Usage = 80 });
                MainContainerViewModel parent = (Parent as MainContainerViewModel);

                LastSyncTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                ProgressValue = WallrutInfo.Info.DiskUsage;

                RegistryKey reg = Registry.CurrentUser;
                reg = reg.OpenSubKey("SOFTWARE\\WALLnut", true);

                if (!Object.ReferenceEquals(reg, null) &&
                    !Object.ReferenceEquals(reg.GetValue("WALLnutFolderPath"), null) &&
                    !reg.GetValue("WALLnutFolderPath").ToString().Equals(string.Empty))
                {
                    DirPath = reg.GetValue("WALLnutFolderPath").ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            OnOffCommand = new DelegateCommand<object>(onOnOff);
            FileSystemCommand = new DelegateCommand<object>(onFileSystem);
            SettingCommand = new DelegateCommand<object>(onSetting);
            LogCommand = new DelegateCommand<object>(onLogHistory);
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();
        }
        
        private void onSetting(object obj)
        {
            if (this.Parent is MainContainerViewModel)
                (this.Parent as MainContainerViewModel).ShowSettingCommand.Execute(null);
        }

        private void onLogHistory(object obj)
        {
            if (this.Parent is MainContainerViewModel)
                (this.Parent as MainContainerViewModel).ShowLogHistoryCommand.Execute(null);
        }

        /// <summary>
        /// [Function] WALLnut 탐색기 창을 엽니다
        /// </summary>
        /// <param name="obj"></param>
        private void onFileSystem(object obj)
        {
            if (this.Parent is MainContainerViewModel)
                (this.Parent as MainContainerViewModel).ShowFileExplorerCommand.Execute(WallrutInfo.Info.FileWatcher.getRoot());
        }

        /// <summary>
        /// [Function] 실시간 파일 동기화를 키고 끕니다
        /// </summary>
        /// <param name="obj"></param>
        private void onOnOff(object obj)
        {
            if (!WallrutInfo.Info.FileWatcher.isWatching)
            {
                WallrutInfo.Info.FileWatcher.DirPath = DirPath;
                WallrutInfo.Info.FileWatcher.Start();
            }
            else
            {
                PathEnabled = true;
                WallrutInfo.Info.FileWatcher.Stop();
            }
        }
    }
}
