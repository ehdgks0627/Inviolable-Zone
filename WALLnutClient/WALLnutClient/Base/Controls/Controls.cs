using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WALLnutClient
{
    #region TreeView
    [FlagsAttribute]
    public enum NodeTypes
    {
        ROOT = 0,
        DRIVE,
        FOLDER,
        FILE,
        GOBACK,
        NONE = 99
    }

    public class TreeNodeCollection : ObservableCollection<ChildTreeNode>
    {
        public ObservableCollection<ChildTreeNode> Hierarchical(ChildTreeNode rootTreeNode)
        {
            ObservableCollection<ChildTreeNode> startTreeGroup = new ObservableCollection<ChildTreeNode>();

            foreach (ChildTreeNode element in this.OrderBy(o => o.NodeType))
            {
                //시작 노드
                if (element.Parent.NodeType == NodeTypes.ROOT)
                {
                    element.Parent = rootTreeNode;
                    startTreeGroup.Add(element);
                    TreeDepth(startTreeGroup[startTreeGroup.Count - 1]);
                }
            }
            return startTreeGroup;
        }

        private void TreeDepth(ChildTreeNode pTreeGroup)
        {
            string nodePath = pTreeGroup.Path + @"\" + pTreeGroup.NodeKey;
            foreach (ChildTreeNode element in this.OrderBy(o => o.NodeType))
            {
                if (element.NodeType == NodeTypes.FOLDER)
                {
                    //시작 노드
                    if (element.Path.Replace("//", "/") == nodePath)
                    {
                        element.Parent = pTreeGroup;
                        pTreeGroup.GroupTreeNodes.Add(element);
                        TreeDepth(pTreeGroup.GroupTreeNodes[pTreeGroup.GroupTreeNodes.Count - 1]);
                    }
                }
            }
            pTreeGroup.ChildNodes.Add(new CollectionContainer() { Collection = pTreeGroup.GroupTreeNodes });
        }
    }

    public class ChildTreeNode : DependencyObject
    {
        public ChildTreeNode()
        {
            ChildNodes = new CompositeCollection();
            GroupTreeNodes = new TreeNodeCollection();
        }

        public NodeTypes NodeType
        {
            get { return (NodeTypes)GetValue(eNodeTypeProperty); }
            set { SetValue(eNodeTypeProperty, value); }
        }
        // Using a DependencyProperty as the backing store for eNodeType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty eNodeTypeProperty =
            DependencyProperty.Register("NodeType", typeof(NodeTypes), typeof(ChildTreeNode), new PropertyMetadata(NodeTypes.ROOT));

        public string NodeKey
        {
            get { return (string)GetValue(NodeKeyProperty); }
            set { SetValue(NodeKeyProperty, value); }
        }
        // Using a DependencyProperty as the backing store for NodeKey.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeKeyProperty =
            DependencyProperty.Register("NodeKey", typeof(string), typeof(ChildTreeNode), new PropertyMetadata(null));

        public string NodeText
        {
            get { return (string)GetValue(NodeTextProperty); }
            set { SetValue(NodeTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeTextProperty =
            DependencyProperty.Register("NodeText", typeof(string), typeof(ChildTreeNode), new PropertyMetadata(null));

        /// <summary>
        /// 노드 확장 상태
        /// </summary>
        public Boolean IsExpanded
        {
            get { return (Boolean)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }
        // Using a DependencyProperty as the backing store for IsExpanded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(Boolean), typeof(ChildTreeNode), new FrameworkPropertyMetadata(false, OnExpanded));

        private static void OnExpanded(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChildTreeNode model = d as ChildTreeNode;
            if (model != null)
                model.OnIsExpandChanged(d, e);
        }
        private void OnIsExpandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (CommandExpandChanged != null)
                CommandExpandChanged.Execute(this);
        }

        /// <summary>
        /// 노드 선택 여부
        /// </summary>
        [DefaultValue(false)]
        public Boolean IsSelected
        {
            get { return (Boolean)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(Boolean), typeof(ChildTreeNode), new PropertyMetadata(null));

        public CompositeCollection ChildNodes
        {
            get { return (CompositeCollection)GetValue(ChildNodesProperty); }
            set { SetValue(ChildNodesProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ChildNodes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChildNodesProperty =
            DependencyProperty.Register("ChildNodes", typeof(CompositeCollection), typeof(ChildTreeNode), new PropertyMetadata(null));

        public TreeNodeCollection GroupTreeNodes
        {
            get { return (TreeNodeCollection)GetValue(GroupTreeNodesProperty); }
            set { SetValue(GroupTreeNodesProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ChildNodes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GroupTreeNodesProperty =
            DependencyProperty.Register("GroupTreeNodes", typeof(TreeNodeCollection), typeof(ChildTreeNode), new PropertyMetadata(null));

        public ChildTreeNode Parent
        {
            get { return (ChildTreeNode)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Parent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.Register("Parent", typeof(ChildTreeNode), typeof(ChildTreeNode), new PropertyMetadata(null));

        [DefaultValue(0)]
        public int SubCount { get; set; }
        public string Path { get; set; }

        public ICommand CommandExpandChanged { get; set; }
        public ICommand ClickCommand { get; set; }



        #region 노드 계층 구조 만들기...
        /// <summary>
        /// 계층 구조로 만듭니다.
        /// </summary>
        public void HierarchicalBuild()
        {
            ChildTreeNode rootTreeNode = null;
            //if (this.NodeType == NodeType.ROOT)
            rootTreeNode = this;

            ObservableCollection<ChildTreeNode> arrParent = GroupTreeNodes.Hierarchical(rootTreeNode);

            ChildNodes.Clear();
            ChildNodes.Add(new CollectionContainer() { Collection = arrParent });
        }
        #endregion //노드 계층 구조 만들기...
    }


    public class TvTreeView : TreeView
    {
        public object SelectedNode
        {
            get { return (object)GetValue(SelectedNodeProperty); }
            set { SetValue(SelectedNodeProperty, value); }
        }
        public static DependencyProperty SelectedNodeProperty = DependencyProperty.Register("SelectedNode", typeof(object), typeof(TvTreeView), new UIPropertyMetadata(null));

        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register("ClickCommand", typeof(ICommand), typeof(TvTreeView), new PropertyMetadata(null));



        public TvTreeView() : base()
        {
            this.SelectedItemChanged += TvTreeView_SelectedItemChanged;
        }

        private void TvTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.SelectedNode = e.NewValue;
        }
    }
    #endregion //TreeView
}
