using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    class FileNode
    {
        public FileNode Root;
        public UInt64 RootIndex;
        public UInt64 index;
        public Dictionary<string, FileNode> child;

        public byte[] buffer;
        string filename { get; set; }
        string fullname { get; set; }

        public unsafe FileNode(DiskManager.ENTRY_FILE_STRUCTURE _info, UInt64 _index)
        {
            buffer = (byte[])_info.Clone();
            child = new Dictionary<string, FileNode>();
            fixed (byte* ptr_buffer = buffer)
            {
                DiskManager.ENTRY_FILE_STRUCTURE* ptr = (DiskManager.ENTRY_FILE_STRUCTURE * )ptr_buffer;
                filename = Marshal.PtrToStringUni((IntPtr)(ptr->filename));
                fullname = FullPath();
                index = _index;
            }
        }

        public FileNode FindNodeByFilename(string path, int deep)
        {
            FileNode c;
            string[] paths = path.Split('\\');
            try
            {
                c = child[paths[deep]];
            }
            catch
            {
                return null;
            }
            if(paths.Length == deep)
            {
                return this;
            }
            else
            {
                return c.FindNodeByFilename(path, deep + 1);
            }
        }

        public bool AppendNode(FileNode node)
        {
            child.Add(node.filename, node);
            return true;
        }

        public string FullPath()
        {
            if(Root != null)
            {
                return Root.FullPath() + "\\" + filename;
            }
            else
            {
                return "\\";
            }
        }
        public UInt64 path2offset()
        {
            return 2;
        }
     }
}
