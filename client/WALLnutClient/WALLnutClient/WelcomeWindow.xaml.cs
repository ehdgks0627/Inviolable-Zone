﻿using System;
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
    public partial class WelcomeWindow : MetroWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            img_logo.Source = new BitmapImage(new Uri(Properties.Resources.RESOURCES_PATH + "logo.png", UriKind.RelativeOrAbsolute));
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            SelectDiskWindow window = new SelectDiskWindow();
            window.Show();
            this.Hide();
        }
    }
}
