using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WALLnutClient
{
    public class FileExplorerViewModel : BaseControlViewModel
    {
        private FreezableCollection<ChildTreeNode> _TreeNodes = new FreezableCollection<ChildTreeNode>();
        public FreezableCollection<ChildTreeNode> TreeNodes
        {
            get { return _TreeNodes; }
            set { SetProperty(ref _TreeNodes, value); }
        }

        //FileNode Root = null;

        public ICommand CommandExpanded { get; set; }
        public ICommand CommandChildTreeNodeClick { get; set; }



        public FileExplorerViewModel()
        {

        }



        #region override

        protected override void InitData(object parameter)
        {
            base.InitData(parameter);

            Title = "파일 탐색기";
            TreeNodes.Clear();
            //Root = _Root;
            //tv_file.Items.Clear();
            //tv_file.Items.Add(Root.GetTreeViewSource());
            RootTreeNode(parameter as FileNode);
        }

        protected override void LoadData(object parameter)
        {
            base.LoadData(parameter);
        }

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            CommandExpanded = new DelegateCommand<object>(onTreeExpanded);
            CommandChildTreeNodeClick = new DelegateCommand<object>(onChildNodeClick);
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();

            CommandExpanded = null;
            CommandChildTreeNodeClick = null;
        }

        #endregion //override



        private void onTreeExpanded(object obj)
        {
        }

        private async void onChildNodeClick(object obj)
        {
            byte[] fileContent = null;
            ChildTreeNode item = (obj as ChildTreeNode);
            WallrutInfo.Info.FileWatcher.ReadFile(item.NodeKey, out fileContent);
            if (!ReferenceEquals(fileContent, null))
            {
                var resultTask = Connection.PostRequest("/v1/user/request-decode-data/", new Dictionary<string, string>());
                var result = await resultTask;
                JObject response = JObject.Parse(result);
                String aes128Key = response["aes128_key"].ToObject<String>();
                fileContent = AES128.Decrypt(fileContent, aes128Key);

                CommonOpenFileDialog dlg = new CommonOpenFileDialog();
                dlg.Title = "Select Directory";
                dlg.IsFolderPicker = true;
                dlg.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

                dlg.AddToMostRecentlyUsedList = false;
                dlg.AllowNonFileSystemItems = false;
                dlg.DefaultDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                dlg.EnsureFileExists = true;
                dlg.EnsurePathExists = true;
                dlg.EnsureReadOnly = false;
                dlg.EnsureValidNames = true;
                dlg.Multiselect = false;
                dlg.ShowPlacesList = true;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string outputPath = dlg.FileName + item.NodeKey;
                    using (FileStream file = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        file.Write(fileContent, 0, fileContent.Length);
                    }
                }
                MessageBox.Show(item.NodeKey + " 백업 완료", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RootTreeNode(FileNode root)
        {
            try
            {
                ChildTreeNode rootNode = null;// = new ChildTreeNode() { NodeType = NodeTypes.ROOT, NodeText = "RootDir", Path = @"C:\WALLnut\", SubCount = 0 };
                if (root != null)
                {
                    rootNode = new ChildTreeNode() { NodeType = NodeTypes.ROOT, NodeText = root.ToString(), Path = root.FullPath(), SubCount = root.child.Count };

                    CreateTreeNodes(rootNode, root);
                }

                rootNode.HierarchicalBuild();
                TreeNodes.Add(rootNode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void CreateTreeNodes(ChildTreeNode rootNode, FileNode root)
        {
            foreach (var node in root.child)
            {
                if (node.Value == null) continue;
                NodeTypes ntType = (node.Value.isFolder()) ? NodeTypes.FOLDER : NodeTypes.FILE;
                string path = node.Value.FullPath();
                ChildTreeNode child = new ChildTreeNode()
                {
                    NodeType = ntType,
                    Parent = rootNode,
                    Path = path.Substring(0, path.LastIndexOf("\\")),
                    NodeKey = node.Value.FullPath(),
                    NodeText = node.Key,
                    SubCount = node.Value.child.Count,
                    CommandExpandChanged = CommandExpanded,
                    ClickCommand = CommandChildTreeNodeClick
                };
                rootNode.GroupTreeNodes.Add(child);

                FileNode childFileNode = (node.Value as FileNode);
                if (childFileNode != null && childFileNode.child.Count > 0)
                    CreateTreeNodes(child, childFileNode);
            }
        }
    }
}
