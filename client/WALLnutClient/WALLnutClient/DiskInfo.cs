using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

namespace WALLnutClient
{
    public class DiskInfo
    {
        public bool isWALLNutDevice { get; set; }
        public string Caption { get; set; }
        public string DeviceID { get; set; }
        public string Model { get; set; }
        public UInt64 Partitions { get; set; }
        public UInt64 Size { get; set; }

        public override string ToString()
        {
            return (isWALLNutDevice ? "[O] " : "[X] ") + Caption + " (" + (Size / 1024 / 1024 / 1024) + "GB)";
        }

        #region [Function] PhysicalDrive의 목록을 반환
        public static List<DiskInfo> GetDriveList()
        {
            List<DiskInfo> result = new List<DiskInfo>();
            Thread t = new Thread(() => { GetDriveListThread(ref result); });
            t.Start();
            t.Join();
            return result;
        }

        private static void GetDriveListThread(ref List<DiskInfo> result)
        {
            byte[] buffer = new byte[DiskManager.BLOCK_SIZE];
            ManagementObjectSearcher searcher =
                                   new ManagementObjectSearcher("",
                                   "SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                DiskInfo diskinfo = new DiskInfo();
                diskinfo.Caption = queryObj["Caption"].ToString();
                diskinfo.DeviceID = queryObj["DeviceID"].ToString();
                diskinfo.Model = queryObj["Model"].ToString();
                diskinfo.Size = ulong.Parse(queryObj["Size"].ToString());

                DiskManager.ReadBlock(ref buffer, 0, diskinfo.DeviceID);
                diskinfo.isWALLNutDevice = true;
                for (int i = 0; i < 8; i++)
                {
                    if (buffer[i] != DiskManager.SIGNATURE[i])
                    {
                        diskinfo.isWALLNutDevice = false;
                        break;
                    }
                }
                result.Add(diskinfo);
            }
        }
        #endregion
    }
}
