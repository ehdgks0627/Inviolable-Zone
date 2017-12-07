using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WALLnutClient
{
    public class SelectDiskWindowViewModel : BindableBase
    {
        public ICommand NextCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        private List<DiskInfo> infos { get; set; }
        public ObservableCollection<DiskInfo> DiskInfoCollection { get; private set; }

        private String _Action = String.Empty;
        public String Action
        {
            get { return _Action; }
            set { SetProperty(ref _Action, value); }
        }

        private DiskInfo _CurrentItem;
        public DiskInfo CurrentItem
        {
            get { return _CurrentItem; }
            set
            {
                _CurrentItem = null;
                SetProperty(ref _CurrentItem, value, "CurrentItem");
                if (!ReferenceEquals(CurrentItem, null) && !CurrentItem.isWALLNutDevice)
                {
                    Action = "Format";
                }
                else
                {
                    Action = "Continue";
                }
            }
        }

        public SelectDiskWindowViewModel()
        {
            NextCommand = new DelegateCommand<object>(onNext);
            RefreshCommand = new DelegateCommand<object>(onRefresh);
            UpdateDriveList();
        }

        #region [Function] 버튼 버튼 핸들러
        private void onNext(object obj)
        {
            if (ReferenceEquals(CurrentItem, null))
            {
                MessageBox.Show("진행할 디스크를 선택해주세요!", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (diskIsConnected(CurrentItem))
            {
                if (CurrentItem.isWALLNutDevice)
                {
                    RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                    regKey.SetValue("WALLnutDriveUUID", CurrentItem.uuid, RegistryValueKind.String);
                    nextWindow(CurrentItem);
                }
                else
                {
                    if (MessageBoxResult.OK.Equals(MessageBox.Show("포맷하시겠습니까? 디스크 내 모든 데이터가 초기화됩니다!", "주의", MessageBoxButton.OKCancel, MessageBoxImage.Warning)))
                    {
                        Int64 uuid = DiskManager.FormatDisk(CurrentItem);
                        if (!uuid.Equals(0))
                        {
                            RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\WALLnut", RegistryKeyPermissionCheck.ReadWriteSubTree);

                            regKey.SetValue("WALLnutDriveUUID", uuid, RegistryValueKind.String);
                            MessageBox.Show("포맷 성공", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                            nextWindow(CurrentItem);
                        }
                    }
                }
            }
            else
            {
                UpdateDriveList();
                MessageBox.Show("디스크를 인식할 수 없습니다", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        private void onRefresh(object obj)
        {
            UpdateDriveList();
        }

        #region [Function] Drive 목록 업데이트
        private void UpdateDriveList(bool updateCurrentItem = true)
        {
            infos = DiskInfo.GetDriveList();
            DiskInfoCollection = new ObservableCollection<DiskInfo>(infos);
            if (updateCurrentItem)
            {
                RaisePropertyChanged("DiskInfoCollection");
                if (infos.Count > 0)
                {
                    CurrentItem = infos[0];
                }
                else
                {
                    CurrentItem = null;
                }
            }
        }
        #endregion

        private Boolean diskIsConnected(DiskInfo info)
        {
            infos = DiskInfo.GetDriveList();
            return infos.Contains(info);
        }

        public bool checkWALLnutDriveExist()
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
                    return true;
                }
            }
            return false;
        }

        private void nextWindow(DiskInfo info)
        {
            SelectDiskWindow select = App.Current.Windows.OfType<SelectDiskWindow>().FirstOrDefault();
            MainWindow window = new MainWindow(info);
            App.Current.MainWindow = window;
            window.Show();

            if (select != null)
                select.Close();
        }
    }
}
