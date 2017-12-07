using System.Windows;


namespace WALLnutClient
{
    public partial class MainWindow : Window
    {
        public MainWindow(DiskInfo info)
        {
            InitializeComponent();

            (windowRoot.MainModule as MainContainerViewModel).Info = info;
            (windowRoot.MainModule as MainContainerViewModel).ShowMainViewCommand.Execute(info);
        }

        private void Header_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}