using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public class WallrutInfo : BindableAndDisposable
    {
        private DiskInfo _DiskInfo = null;
        public DiskInfo DiskInfo {
            get { return _DiskInfo; }
            private set { SetProperty(ref _DiskInfo, value); }
        }

        private FileWatcher _FileWatcher = null;
        public FileWatcher FileWatcher
        {
            get { return _FileWatcher; }
            private set { SetProperty(ref _FileWatcher, value); }
        }

        public Double DiskUsage
        {
            get {
                if (FileWatcher == null || DiskInfo == null)
                    return 0;
                else
                    return (FileWatcher.getDiskUsage() / DiskInfo.size);
            }
        }

        private static object sync = new Object();
        private static WallrutInfo _Info;
        /// <summary>
        /// 싱글턴 객체
        /// </summary>
        public static WallrutInfo Info
        {
            get
            {
                lock (sync)
                {
                    if (_Info == null)
                        _Info = new WallrutInfo();
                }
                return _Info;
            }
        }

        private WallrutInfo() { }


        protected override void DisposeManaged()
        {
            if (_Info != null)
                _Info.Dispose();
            if (FileWatcher != null)
                FileWatcher.Stop();

            base.Dispose();
        }



        public void Initial(DiskInfo disk)
        {
            DiskInfo = disk;
            FileWatcher = new FileWatcher(disk);
        }
    }
}
