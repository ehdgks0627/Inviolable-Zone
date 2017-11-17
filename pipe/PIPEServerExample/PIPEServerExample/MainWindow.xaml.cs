using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PIPEServerExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Run()
        {
            PipeServer serv = new PIPEServerExample.PipeServer(this);
            serv.PipeName = "WALLnut";
            serv.Run();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Thread t1 = new Thread(new ThreadStart(Run));
            t1.Start();
            MessageBox.Show("Start");
        }
    }
}
