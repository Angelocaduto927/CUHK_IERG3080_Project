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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project
{
    public partial class MainWindow : Window
    {
        private const double AspectRatio = 16.0 / 9.0;
        private bool _resizing = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AudioManager.Cleanup();
        }

        // This is the handler referenced in XAML
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_resizing) return;
            _resizing = true;

            if (e.WidthChanged)
                this.Height = this.Width / AspectRatio;
            else if (e.HeightChanged)
                this.Width = this.Height * AspectRatio;

            _resizing = false;
        }
    }
}

