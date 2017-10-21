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

        #region [Function] [생성자] 파일 노드의 정보를 셋팅합니다
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
                DiskManager.ENTRY_FILE_STRUCTURE* ptr = (DiskManager.ENTRY_FILE_STRUCTURE*)ptr_buffer;
                filename = Marshal.PtrToStringUni((IntPtr)(ptr->filename));
                fullname = null;
                type = ptr->type;
                index = _index;
            }
        }
        #endregion

        #region [Function] 파일 이름을 기준으로 노드를 찾습니다
        public FileNode FindNodeByFilename(string path, int deep, bool isparent = false)
        {
            if (path.Equals(string.Empty) || !path[0].Equals('\\'))
            {
                return null;
            }
            else
            {
                path = path.Substring(1, path.Length - 1);
            }
            if (!path.Length.Equals(0) && path[path.Length - 1].Equals('\\'))
            {
                path = path.Substring(0, path.Length - 1);
            }
            string[] paths = path.Split('\\');
            if (paths[deep].Equals(filename) && paths.Length.Equals(deep + 1))
            {
                return this;
            }
            return this._FindNodeByFilename(paths, deep, isparent);
        }
        #endregion

        #region [Function] 파일 이름을 찾는데 내부적으로 사용되는 함수입니다
        private FileNode _FindNodeByFilename(string[] paths, int deep, bool isparent = false)
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
            if (paths[deep].Equals(c.filename) && paths.Length.Equals(deep + 1))
            {
                if (isparent)
                {
                    if (c.type.Equals(DiskManager.BLOCKTYPE.ENTRY_FOLDER))
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

        #endregion
        #region [Function] 자식에 노드를 추가합니다
        public bool AppendChild(FileNode node)
        {
            child.Add(node.filename, node);
            node.Root = this;
            node.RootIndex = this.index;
            node.fullname = node.FullPath();
            return true;
        }
        #endregion

        #region [Function] 현재 노드의 절대 경로를 반환합니다
        public string FullPath()
        {
            if (!Object.ReferenceEquals(Root, null))
            {
                return Root.FullPath() + "\\" + filename;
            }
            else
            {
                return filename;
            }
        }
        #endregion

        #region [Function] 기존에 있었던 노드를 새로운 정보로 업데이트 합니다
        public void UpdateInfo(DiskManager.ENTRY_FILE_STRUCTURE _info)
        {
            int size = Marshal.SizeOf(_info);
            buffer = new byte[size];
            IntPtr tmp_ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(_info, tmp_ptr, true);
            Marshal.Copy(tmp_ptr, buffer, 0, size);
            Marshal.FreeHGlobal(tmp_ptr);
        }
        #endregion

        #region [Function] 파일을 지웁니다
        public void DeleteNode(DiskManager manager)
        {
            this._DeleteNode(manager);
            if (!Object.ReferenceEquals(Root, null))
            {
                Root.child.Remove(filename);
            }
        }
        #endregion

        #region [Function] 파일을 지우는데 내부적으로 사용되는 함수입니다
        private void _DeleteNode(DiskManager manager)
        {
            manager._DeleteFile(fullname);
            foreach (string key in child.Keys)
            {
                child[key]._DeleteNode(manager);
            }
            child.Clear();
        }
        #endregion

        public override string ToString()
        {
            return filename;
        }

        #region [Function] 트리뷰를 볼 수 있도록 가공합니다
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
        #endregion
    }
}
