using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;

namespace WALLnutClient
{
    public class DiskInfo
    {
        public bool isWALLNutDevice { get; set; }
        public string caption { get; set; }
        public string deviceID { get; set; }
        public string model { get; set; }
        public UInt64 partitions { get; set; }
        public UInt64 size { get; set; }
        public Int64 uuid { get; set; }

        public override string ToString()
        {
            return (isWALLNutDevice ? "[O] " : "[X] ") + caption + " (" + (size / 1024 / 1024 / 1024) + "GB)";
        }

        #region [Function] PhysicalDrive의 목록을 반환
        public unsafe static List<DiskInfo> GetDriveList()
        {
            List<DiskInfo> infos = new List<DiskInfo>();
            byte[] buffer = new byte[DiskManager.BLOCK_SIZE];
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("", "SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                try
                {
                    DiskInfo diskinfo = new DiskInfo();
                    diskinfo.caption = queryObj["Caption"].ToString();
                    diskinfo.deviceID = queryObj["DeviceID"].ToString();
                    diskinfo.model = queryObj["Model"].ToString();
                    diskinfo.size = ulong.Parse(queryObj["Size"].ToString());
                    DiskManager.ReadBlock(ref buffer, 0, diskinfo.deviceID);
                    diskinfo.isWALLNutDevice = true;
                    for (int i = 0; i < 8; i++)
                    {
                        if (buffer[i] != DiskManager.SIGNATURE[i])
                        {
                            diskinfo.isWALLNutDevice = false;
                            diskinfo.uuid = -1;
                            break;
                        }
                    }
                    if (diskinfo.isWALLNutDevice)
                    {
                        fixed (byte* ptrBuffer = &buffer[0])
                        {
                            DiskManager.DISK_HEADER_STRUCTURE* ptr = (DiskManager.DISK_HEADER_STRUCTURE*)ptrBuffer;
                            diskinfo.uuid = ptr->uuid;
                        }
                    }
                    infos.Add(diskinfo);
                }
                catch
                {
                    continue;
                }
            }
            return infos;
        }
        #endregion

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(obj, null))
            {
                return false;
            }
            if (obj.GetType().Equals(typeof(DiskInfo)))
            {
                return (obj as DiskInfo).uuid.Equals(uuid);
            }
            else
            {
                return false;
            }
        }
    }
}
