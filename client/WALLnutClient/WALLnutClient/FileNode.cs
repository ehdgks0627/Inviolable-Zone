using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WALLnutClient
{
    public class FileNode
    {
        public static Int64 LastUpdate;
        public FileNode Root;
        public UInt64 RootIndex;
        public UInt64 index;
        public Dictionary<string, FileNode> child;
        public DiskManager.BLOCKTYPE type;

        public byte[] buffer;
        string filename { get; set; }
        string fullname { get; set; }

        public unsafe FileNode(DiskManager.ENTRY_FILE_STRUCTURE _info, UInt64 _index)
        {
            int size = Marshal.SizeOf(_info);
            buffer = new byte[size];
            IntPtr tmp_ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(_info, tmp_ptr, true);
            Marshal.Copy(tmp_ptr, buffer, 0, size);
            Marshal.FreeHGlobal(tmp_ptr);

            child = new Dictionary<string, FileNode>();
            fixed (byte* ptr_buffer = buffer)
            {
                DiskManager.ENTRY_FILE_STRUCTURE* ptr = (DiskManager.ENTRY_FILE_STRUCTURE * )ptr_buffer;
                filename = Marshal.PtrToStringUni((IntPtr)(ptr->filename));
                fullname = null;
                type = ptr->type;
                index = _index;
            }
        }

        public FileNode FindNodeByFilename(string path, int deep, bool isparent=false)
        {
            if(path[0] != '\\')
            {
                return null;
            }
            else
            {
                path = path.Substring(1, path.Length - 1);
            }
            if (path.Length != 0 && path[path.Length - 1] == '\\')
            {
                path = path.Substring(0, path.Length - 1);
            }
            string[] paths = path.Split('\\');
            if(paths[deep] == filename && paths.Length == (deep +1))
            {
                return this;
            }
            return this._FindNodeByFilename(paths, deep, isparent);
        }

        private FileNode _FindNodeByFilename(string[] paths, int deep, bool isparent=false)
        {
            FileNode c;
            try
            {
                c = child[paths[deep]];
            }
            catch
            {
                return null;
            }
            if(paths[deep] == c.filename && paths.Length == (deep + 1))
            {
                if (isparent)
                {
                    if(c.type == DiskManager.BLOCKTYPE.ENTRY_FOLDER)
                    {
                        return c;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return c;
                }
            }
            return c._FindNodeByFilename(paths, deep + 1, isparent);
        }

        public bool AppendChild(FileNode node)
        {
            child.Add(node.filename, node);
            node.Root = this;
            node.RootIndex = this.index;
            node.fullname = node.FullPath();
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
                return "";
            }
        }

        public void UpdateInfo(DiskManager.ENTRY_FILE_STRUCTURE _info)
        {
            int size = Marshal.SizeOf(_info);
            buffer = new byte[size];
            IntPtr tmp_ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(_info, tmp_ptr, true);
            Marshal.Copy(tmp_ptr, buffer, 0, size);
            Marshal.FreeHGlobal(tmp_ptr);
        }

        public void DeleteNode(DiskManager manager)
        {
            this._DeleteNode(manager);
            if (Root != null)
            {
                Root.child.Remove(filename);
            }
        }

        private void _DeleteNode(DiskManager manager)
        {
            manager._DeleteFile(fullname);
            foreach (string key in child.Keys)
            {
                child[key]._DeleteNode(manager);
            }
            child.Clear();
        }

        public override string ToString()
        {
            return filename;
        }

        public TreeViewItem GetTreeViewSource()
        {
            TreeViewItem result = new TreeViewItem();
            foreach (string key in child.Keys)
            {
                result.Items.Add(child[key].GetTreeViewSource());
            }
            result.Header = filename;
            return result;
        }
    }
}
