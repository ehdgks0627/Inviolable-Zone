using MahApps.Metro.Controls;
using System.Windows;

namespace WALLnutClient
{
    /// <summary>
    /// Interaction logic for BlackWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            //this.Hide();
            InitializeComponent();
            this.DataContext = new WelcomeWindowViewModel();
        }

        private void Header_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
