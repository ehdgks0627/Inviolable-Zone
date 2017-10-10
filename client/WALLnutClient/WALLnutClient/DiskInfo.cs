using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return (isWALLNutDevice ? "[O] " : "[X] ") + Caption + " (" + (Size/1024/1024/1024) + "GB)";
        }

        #region [Function] PhysicalDrive의 목록을 반환
        public static List<DiskInfo> GetDriveList()
        {
            List<DiskInfo> result = new List<DiskInfo>();
            Process WmicProcess = new Process();
            byte[] buffer = new byte[DiskManager.BLOCK_SIZE];
            WmicProcess.StartInfo.FileName = "wmic.exe";
            WmicProcess.StartInfo.UseShellExecute = false;
            WmicProcess.StartInfo.Arguments = "diskdrive list brief / format:list";
            WmicProcess.StartInfo.RedirectStandardOutput = true;
            WmicProcess.StartInfo.CreateNoWindow = true;
            WmicProcess.Start();

            string[] lines = WmicProcess.StandardOutput.ReadToEnd().Split(new[] { "\r\r\n\r\r\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                DiskInfo diskinfo = new DiskInfo();
                string[] infos = line.Split(new[] { "\r\r\n" }, StringSplitOptions.None);
                foreach (string info in infos)
                {
                    try
                    {
                        string[] t = info.Split('=');
                        if (t[0] == "Caption")
                        {
                            diskinfo.Caption = t[1];
                        }
                        else if (t[0] == "DeviceID")
                        {
                            diskinfo.DeviceID = t[1];
                        }
                        else if (t[0] == "Model")
                        {
                            diskinfo.Model = t[1];
                        }
                        else if (t[0] == "Partitions")
                        {
                            diskinfo.Partitions = Convert.ToUInt64(t[1]);
                        }
                        else if (t[0] == "Size")
                        {
                            diskinfo.Size = Convert.ToUInt64(t[1]);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                DiskManager.ReadBlock(ref buffer, 0, diskinfo.DeviceID);
                diskinfo.isWALLNutDevice = true;
                for(int i=0; i<8; i++)
                {
                    if(buffer[i] != DiskManager.SIGNATURE[i])
                    {
                        diskinfo.isWALLNutDevice = false;
                        break;
                    }
                }
                result.Add(diskinfo);
            }
            WmicProcess.WaitForExit();
            WmicProcess.Close();
            return result;
        }
        #endregion
    }
}
