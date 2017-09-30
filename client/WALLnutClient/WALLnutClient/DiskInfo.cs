using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public class DiskInfo
    {
        public string Caption { get; set; }
        public string DeviceID { get; set; }
        public string Model { get; set; }
        public UInt64 Partitions { get; set; }
        public UInt64 Size { get; set; }

        public override string ToString()
        {
            return Caption + " (" + (Size/1024/1024/1024) + "GB)";
        }
    }
}
