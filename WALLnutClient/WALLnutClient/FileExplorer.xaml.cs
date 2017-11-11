using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace WALLnutClient
{
    /// <summary>
    /// Interaction logic for BlackWindow.xaml
    /// </summary>
    public partial class FileExplorer : MetroWindow
    {
        FileNode Root = null;
        public FileExplorer(FileNode _Root)
        {
            InitializeComponent();
            Root = _Root;
            tv_file.Items.Clear();
            tv_file.Items.Add(Root.GetTreeViewSource());
        }
    }
}
