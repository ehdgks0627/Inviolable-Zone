using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public class FileFeature
    {
        public string mimeType { get; set; }
        public byte[] data { get; set; }
        public string path { get; set; }
        public string oldPath { get; set; }
        public bool isFolder { get; set; }
        public DiskLog.TYPE method { get; set; }

        public override string ToString()
        {
            return path;
        }
    }
}
