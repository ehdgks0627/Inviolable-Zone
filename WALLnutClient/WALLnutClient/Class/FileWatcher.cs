using HeyRed.Mime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WALLnutClient
{
    public delegate void DiskLogEventHandler(DiskLog diskLog);



    public class FileWatcher : IDisposable
    {
        public string DirPath { get; set; }
        public Boolean isWatching { get; set; }

        private List<string> blackListExtensions = new List<string>();
        private List<FileFeature> fileTaskQueue = new List<FileFeature>();
        private DiskLog lastLog = new DiskLog(DiskLog.TYPE.CREATE, false, "");
        private const int DELAY_CHANGE = 5000000;
        private FileSystemWatcher fs = null;
        private Boolean isBlocked = false;
        private Boolean isUploading = false;
        private DiskManager manager;
        private Thread checkThread;

        public event DiskLogEventHandler addDiskLog;



        #region [Function] 생성자
        public FileWatcher(DiskInfo info)
        {
            try
            {
                //TestCase(info.deviceID);
                checkThread = new Thread(new ThreadStart(CheckFileFeature));
                checkThread.IsBackground = true;
                checkThread.Start();
                isBlocked = false;
                isWatching = false;
                manager = new DiskManager(info.deviceID);
                if(!manager.isActive)
                {
                    MessageBox.Show("디스크에 접근할 수 없습니다", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //DiskUsage.Add(new DiskUsage() { Category = "정상파일", Usage = 3 });
                //DiskUsage.Add(new DiskUsage() { Category = "감염파일", Usage = 0 });
                //DiskUsage.Where(x => x.Category.Equals("감염파일")).FirstOrDefault().Usage = 1; //TODO not updating
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        #endregion

        #region [Funciton] test function
        public void TestCase(string drivename)
        {
            Debug.Assert(!DiskManager.FormatDisk(new DiskInfo { deviceID = drivename }).Equals(false));
            manager = new DiskManager(drivename);
            Debug.Assert(manager.isActive.Equals(true));
            Debug.Assert(manager.PathToOffset(@"a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.PathToOffset(@"\a") == DiskManager.BLOCK_END);
            Debug.Assert(manager.PathToOffset(@"\") == 2);

            /*for(ulong i=3; i<40000; i++)
            {
                Debug.Assert(manager.AvailableBlock(DiskManager.BLOCKTYPE.DATA) == i);
                Console.WriteLine(i);
            }*/
            byte[] data = new byte[200];
            byte[] read_data;
            Random r = new Random((int)(DateTime.Now.ToFileTimeUtc()));
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(r.Next(1, 255));
            }

            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", data, "").Equals(true));
            Debug.Assert(manager.PathToOffset(@"\test") == 3);
            Debug.Assert(manager.WriteFile(@"\test\a", data, "").Equals(false));
            Debug.Assert(manager.PathToOffset(@"\test") == 3);
            Debug.Assert(manager.WriteFolder(@"\test\a").Equals(false));
            Debug.Assert(manager.WriteFile(@"\test\a\wow", data, "").Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", data, "").Equals(true));
            Debug.Assert(manager.PathToOffset(@"\test") == 3);
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(true));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }

            Debug.Assert(manager.WriteFile(@"\테스트", data, "").Equals(true));
            Debug.Assert(manager.ReadFile(@"\테스트", out read_data).Equals(true));
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            //Debug.Assert(manager.DeleteFile(@"\테스트").Equals(true));

            Debug.Assert(manager.DeleteFile(@"\test").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(false));
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(false));
            Debug.Assert(manager.WriteFile(@"\test", data, "").Equals(true));
            Debug.Assert(manager.ReadFile(@"\test", out read_data).Equals(true));
            Debug.Assert(manager.PathToOffset(@"\test") == 3);
            for (int i = 0; i < data.Length; i++)
            {
                Debug.Assert(data[i] == read_data[i]);
            }
            Debug.Assert(manager.DeleteFile(@"test").Equals(false));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(true));
            Debug.Assert(manager.DeleteFile(@"\test").Equals(false));

            Debug.Assert(manager.WriteFile(@"\asdf\test", data, "").Equals(false));
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
            Debug.Assert(manager.WriteFile(@"\asdf\test", data, "").Equals(true));
            Debug.Assert(manager.WriteFolder(@"\asdf\test").Equals(false));
            Debug.Assert(manager.WriteFile(@"\asdf", data, "").Equals(false));
            Debug.Assert(manager.WriteFile(@"\asdf\\", data, "").Equals(false));
            Debug.Assert(manager.WriteFile(@"asdf\test", data, "").Equals(false));
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
        
        #region [Function] FileSystemSatcher 이벤트 핸들러
        protected void event_CreateFile(object fscreated, FileSystemEventArgs eventOCC)
        {
            bool isFolder = false;
            string mimeType = string.Empty;
            if (isWatching && !isBlocked)
            {
                if (blackListExtensions.Contains(System.IO.Path.GetExtension(eventOCC.Name)))
                {
                    Blocking();
                    return;
                }
                try
                {
                    isFolder = this.isFolder(eventOCC.FullPath);
                    lastLog.fileName = eventOCC.Name;
                    lastLog.time = DateTime.Now.ToFileTimeUtc();
                    lastLog.type = DiskLog.TYPE.CREATE;
                    lastLog.isFolder = isFolder;
                    if (addDiskLog != null)
                        addDiskLog(new DiskLog(DiskLog.TYPE.CREATE, isFolder, eventOCC.Name));
                   
                    if (!isFolder)
                    {
                        mimeType = GetMime(eventOCC.FullPath);
                    }
                    while (isUploading) ;
                    if(!isFolder)
                    {
                        fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.CREATE, mimeType = mimeType, isFolder = isFolder, path = eventOCC.Name, data = File.ReadAllBytes(eventOCC.FullPath) });
                    }
                    else
                    {
                        fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.CREATE, mimeType = mimeType, isFolder = isFolder, path = eventOCC.Name });
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        protected void event_ChangeFile(object fschanged, FileSystemEventArgs eventOCC)
        {
            bool isFolder = false;
            string mime = string.Empty;
            if (isWatching && !isBlocked)
            {
                if (blackListExtensions.Contains(System.IO.Path.GetExtension(eventOCC.Name)))
                {
                    Blocking();
                    return;
                }
                try
                {
                    isFolder = this.isFolder(eventOCC.FullPath);
                    if (eventOCC.Name.Equals(lastLog.fileName) && (lastLog.time + DELAY_CHANGE) > DateTime.Now.ToFileTimeUtc() && lastLog.isFolder.Equals(isFolder))
                    {
                        return;
                    }
                    if (isFolder &&
                       (eventOCC.Name + @"\" + lastLog.fileName.Split('\\')[lastLog.fileName.Split('\\').Length - 1]).Equals(lastLog.fileName) &&
                       (lastLog.time + DELAY_CHANGE) > DateTime.Now.ToFileTimeUtc())
                    {
                        return;
                    }
                    lastLog.fileName = eventOCC.Name;
                    lastLog.time = DateTime.Now.ToFileTimeUtc();
                    lastLog.type = DiskLog.TYPE.CHANGE;
                    lastLog.isFolder = isFolder;

                    if (addDiskLog != null)
                        addDiskLog(new DiskLog(DiskLog.TYPE.CHANGE, isFolder, eventOCC.Name));
                    if (!isFolder)
                    {
                        mime = GetMime(eventOCC.FullPath);
                    }
                    while (isUploading) ;
                    if(!isFolder)
                    {
                        fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.CHANGE, mimeType = mime, isFolder = isFolder, path = eventOCC.Name, data = File.ReadAllBytes(eventOCC.FullPath) });
                    }
                    else
                    {
                        fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.CHANGE, mimeType = mime, isFolder = isFolder, path = eventOCC.Name });
                    }
                }
                catch
                {
                }
                finally
                {
                }
            }
        }

        protected void event_RenameFile(object fschanged, RenamedEventArgs eventOCC)
        {
            bool isFolder = false;
            if (isWatching && !isBlocked)
            {
                try
                {
                    isFolder = this.isFolder(eventOCC.FullPath);
                    lastLog.fileName = eventOCC.Name;
                    lastLog.time = DateTime.Now.ToFileTimeUtc();
                    lastLog.type = DiskLog.TYPE.RENAME;
                    lastLog.isFolder = isFolder;

                    if (blackListExtensions.Contains(System.IO.Path.GetExtension(eventOCC.Name)))
                    {
                        Blocking();
                        return;
                    }
                    if (addDiskLog != null)
                        addDiskLog(new DiskLog(DiskLog.TYPE.RENAME, isFolder, eventOCC.Name, eventOCC.OldName));
                    while (isUploading) ;
                    fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.RENAME, isFolder = isFolder, path = eventOCC.Name, oldPath = eventOCC.OldName });
                }
                catch
                {
                }
            }
        }

        protected void event_DeleteFile(object fschanged, FileSystemEventArgs eventOCC)
        {
            bool isFolder = false;
            if (isWatching && !isBlocked)
            {
                try
                {
                    lastLog.fileName = eventOCC.Name;
                    lastLog.time = DateTime.Now.ToFileTimeUtc();
                    lastLog.type = DiskLog.TYPE.DELETE;
                    lastLog.isFolder = isFolder;

                    if (addDiskLog != null)
                        addDiskLog(new DiskLog(DiskLog.TYPE.DELETE, isFolder, eventOCC.Name));
                    while (isUploading) ;
                    fileTaskQueue.Add(new FileFeature { method = DiskLog.TYPE.DELETE, mimeType = "", isFolder = isFolder, path = eventOCC.Name });
                }
                catch
                {
                }
            }
        }
        #endregion

        #region [Function] 실시간 동기화를 시작합니다
        public void Start()
        {
            if (ReferenceEquals(DirPath, null))
            {
                MessageBox.Show("경로가 설정되지 않았습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!ReferenceEquals(fs, null))
            {
                MessageBox.Show("이미 실행중입니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                fs = new FileSystemWatcher(DirPath, "*.*");

                fs.Created += new FileSystemEventHandler(event_CreateFile);
                fs.Changed += new FileSystemEventHandler(event_ChangeFile);
                fs.Renamed += new RenamedEventHandler(event_RenameFile);
                fs.Deleted += new FileSystemEventHandler(event_DeleteFile);

                fs.EnableRaisingEvents = true;
                fs.IncludeSubdirectories = true;
                fs.InternalBufferSize = 1024 * 64;
                fs.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite;

                isWatching = true;
                MessageBox.Show("동기화 시작!", "시작", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("올바른 경로가 아님...", "에러", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region [Function] 실시간 동기화를 멈춥니다
        public void Stop()
        {
            if (!ReferenceEquals(fs, null))
            {
                fs.Dispose();
                fs.EnableRaisingEvents = false;
                fs = null;
                isWatching = false;
            }
            MessageBox.Show("동기화 종료!", "종료", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #region [Function] Mime 타입의 파일 형식을 반환합니다
        public string GetMime(string path)
        {
            try
            {
                return MimeGuesser.GuessMimeType(path);
            }
            catch (System.BadImageFormatException e)
            {
                return "Please Build Target x64(" + e + ")";
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                return "Unknown";
            }
        }
        #endregion

        #region [Function] 랜섬웨어 감염 파일이 감지되었을때 동기화를 중지합니다
        private void Blocking()
        {
            MessageBox.Show("랜섬웨어 감염 파일이 탐지되었습니다.\n디스크 접근을 차단합니다.\n", "랜섬웨어 탐지", MessageBoxButton.OK, MessageBoxImage.Warning);
            isBlocked = true;
        }
        #endregion

        #region [Function] 디스크 사용량을 반환합니다
        public double getDiskUsage()
        {
            return manager.getUsage();
        }
        #endregion

        #region [Function] root FileNode를 반환합니다
        public FileNode getRoot()
        {
            return manager.root;
        }
        #endregion

        #region [Function] [Thread] 3초를 텀으로 서버에게 파일 정보를 보내 체크합니다
        private async void CheckFileFeature()
        {
            while (true)
            {
                Thread.Sleep(3000);
                if (!fileTaskQueue.Count.Equals(0))
                {
                    isUploading = true;
                    List<FileFeature> queue = new List<FileFeature>(fileTaskQueue);
                    fileTaskQueue.Clear();
                    isUploading = false;
                    JArray features = new JArray();
                    foreach (FileFeature feature in queue)
                    {
                        if (!feature.method.Equals(DiskLog.TYPE.CHANGE) || feature.data.Length < 40 || ReferenceEquals(manager.root.FindNodeByFilename(@"\" + feature.path), null))
                        {
                            continue;
                        }
                        JArray item = new JArray();
                        JArray data = new JArray();
                        for (int i = 0; i < 20; i++)
                        {
                            data.Add(feature.data[i]);
                        }
                        for (int i = 0; i < 20; i++)
                        {
                            data.Add(feature.data[feature.data.Length - 20 + i]);
                        }

                        item.Add(manager.root.FindNodeByFilename(@"\" + feature.path).mimeType);
                        item.Add(data);
                        features.Add(item);
                    }

                    Dictionary<String, String> body = new Dictionary<String, String>
                    {
                       { "request_id", "1" },
                       { "features",  features.ToString().Replace("\r\n","") }
                    };

                    var resultTask = Connection.PostRequest("/v1/wallnut/check-file/", body);
                    var result = await resultTask;

                    
                    if (result.Equals(""))
                    {
                        Console.WriteLine("Err");
                        return;
                    }
                    else
                    {
                        JObject response = JObject.Parse(result);
                        Boolean isInfected = response["isInfected"].ToObject<Boolean>();
                        if (isInfected)
                        {
                            Blocking();
                        }
                        else
                        {
                            String aes128Key = response["aes128_key"].ToObject<String>();
                            foreach (FileFeature feature in queue)
                            {
                                string mimeType = feature.mimeType;
                                byte[] encryptedData = null;
                                if (!ReferenceEquals(feature.data, null))
                                {
                                    encryptedData = AES128.Encrypt(feature.data, aes128Key);
                                }
                                switch (feature.method)
                                {
                                    case DiskLog.TYPE.CREATE:
                                        if (feature.isFolder)
                                        {
                                            manager.WriteFolder(@"\" + feature.path);
                                        }
                                        else
                                        {
                                            manager.WriteFile(@"\" + feature.path, encryptedData, mimeType);
                                        }
                                        break;
                                    case DiskLog.TYPE.CHANGE:
                                        //TODO err, CHANGE 후 
                                        if (feature.isFolder)
                                        {
                                            manager.WriteFolder(@"\" + feature.path);
                                        }
                                        else
                                        {
                                            manager.WriteFile(@"\" + feature.path, encryptedData, mimeType);
                                        }
                                        break;
                                    case DiskLog.TYPE.RENAME:
                                        manager.Rename(@"\" + feature.oldPath, @"\" + feature.path);
                                        break;
                                    case DiskLog.TYPE.DELETE:
                                        manager.DeleteFile(@"\" + feature.path);
                                        break;
                                    case DiskLog.TYPE.ERROR:
                                        //TODO err log*
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (addDiskLog != null)
                addDiskLog = null;
            if (checkThread.IsAlive)
                checkThread.Abort();
        }
        #endregion

        #region [Function] 경로를 기준으로 파일을 읽습니다
        public void ReadFile(string path, out byte[] buffer)
        {
            manager.ReadFile(path, out buffer);
        }
        #endregion
    }
}
