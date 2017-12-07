using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        private void onChildNodeClick(object obj)
        {
        }



        private void RootTreeNode(FileNode root)
        {
            try
            {
                ChildTreeNode rootNode = new ChildTreeNode() { NodeType = NodeTypes.ROOT, NodeText = "RootDir", Path = @"D:\DownLoad\[Image]", SubCount = 0 };
                if (root != null)
                {
                    rootNode = new ChildTreeNode() { NodeType = NodeTypes.ROOT, NodeText = root.ToString(), Path = root.FullPath(), SubCount = root.child.Count };

                    //CreateTreeNodes(rootNode, root);
                }
                SetTreeItems(rootNode);

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
                NodeTypes ntType = (node.Key.Contains('.')) ? NodeTypes.FILE : NodeTypes.FOLDER;
                string path = node.Value.FullPath();
                ChildTreeNode child = new ChildTreeNode()
                {
                    NodeType = ntType,
                    Parent = rootNode,
                    Path = path.Substring(0, path.LastIndexOf("\\")),
                    NodeKey = node.Key,
                    NodeText = node.Key,
                    SubCount = node.Value.child.Count,
                    CommandExpandChanged = CommandExpanded
                };
                rootNode.GroupTreeNodes.Add(child);

                FileNode childFileNode = (node.Value as FileNode);
                if (childFileNode != null && childFileNode.child.Count > 0)
                    CreateTreeNodes(rootNode, childFileNode);
            }
        }

        private void SetTreeItems(ChildTreeNode rootNode)
        {
            try
            {
                if (rootNode != null)
                {
                    foreach (string s in Directory.GetDirectories(rootNode.Path))
                    {
                        DirectoryInfo info = new DirectoryInfo(s);
                        if (info.Attributes.HasFlag(FileAttributes.Hidden) ||
                            info.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                            continue;
                        try
                        {

                            ChildTreeNode child = new ChildTreeNode()
                            {
                                NodeType = NodeTypes.FOLDER,
                                Parent = rootNode,
                                NodeKey = s.Substring(s.LastIndexOf("\\") + 1),
                                NodeText = s.Substring(s.LastIndexOf("\\") + 1),
                                Path = s.Substring(0, s.LastIndexOf("\\")),
                                SubCount = Directory.GetDirectories(s).Length+ Directory.GetFiles(s).Length,
                                CommandExpandChanged = CommandExpanded,
                                ClickCommand = CommandChildTreeNodeClick
                            };
                            rootNode.GroupTreeNodes.Add(child);
                            if (Directory.GetDirectories(s).Count() > 0 || Directory.GetFiles(s).Count() > 0)
                                SubDirFiles(rootNode, child, s);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    foreach (string file in Directory.GetFiles(rootNode.Path))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                            fileInfo.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                            continue;
                        try
                        {
                            ChildTreeNode fileChild = new ChildTreeNode()
                            {
                                NodeType = NodeTypes.FILE,
                                Parent = rootNode,
                                NodeKey = file.Substring(file.LastIndexOf("\\") + 1),
                                NodeText = file.Substring(file.LastIndexOf("\\") + 1),
                                Path = file.Substring(0, file.LastIndexOf("\\")),
                                SubCount = 0,
                                CommandExpandChanged = CommandExpanded,
                                ClickCommand = CommandChildTreeNodeClick
                            };
                            rootNode.GroupTreeNodes.Add(fileChild);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void SubDirFiles(ChildTreeNode rootNode, ChildTreeNode node, string s)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(s))
                {
                    DirectoryInfo info = new DirectoryInfo(dir);
                    if (info.Attributes.HasFlag(FileAttributes.Hidden) ||
                        info.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                        continue;
                    try
                    {
                        ChildTreeNode dirChild = new ChildTreeNode()
                        {
                            NodeType = NodeTypes.FOLDER,
                            Parent = node,
                            NodeKey = dir.Substring(dir.LastIndexOf("\\") + 1),
                            NodeText = dir.Substring(dir.LastIndexOf("\\") + 1),
                            Path = dir.Substring(0, dir.LastIndexOf("\\")),
                            SubCount = Directory.GetDirectories(dir).Length + Directory.GetFiles(dir).Length,
                            CommandExpandChanged = CommandExpanded,
                            ClickCommand = CommandChildTreeNodeClick
                        };
                        rootNode.GroupTreeNodes.Add(dirChild);
                        if (Directory.GetDirectories(dir).Count() > 0 || Directory.GetFiles(dir).Count() > 0)
                            SubDirFiles(rootNode, dirChild, dir);
                    }
                    catch (Exception) {
                        continue;
                    }
                }
                foreach (string file in Directory.GetFiles(s))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                        fileInfo.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                        continue;
                    try
                    {
                        ChildTreeNode fileChild = new ChildTreeNode()
                        {
                            NodeType = NodeTypes.FILE,
                            Parent = node,
                            NodeKey = file.Substring(file.LastIndexOf("\\") + 1),
                            NodeText = file.Substring(file.LastIndexOf("\\") + 1),
                            Path = file.Substring(0, file.LastIndexOf("\\")),
                            SubCount = 0,
                            CommandExpandChanged = CommandExpanded,
                            ClickCommand = CommandChildTreeNodeClick
                        };
                        node.GroupTreeNodes.Add(fileChild);
                    }
                    catch (Exception) {
                        continue;
                    }
                }
                node.HierarchicalBuild();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
