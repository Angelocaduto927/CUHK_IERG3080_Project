using System;
using System.Windows;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project
{
    public partial class MainWindow : Window
    {
        private const double AspectRatio = 16.0 / 9.0; // Aspect ratio for resizing
        private bool _resizing = false; // Flag to prevent unnecessary recursion while resizing

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing; // Subscribe to the window closing event

            // Initialize the AudioManager
            AudioManager.Initialize();
        }

        // Event handler for the window closing event
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AudioManager.Cleanup(); // Cleanup resources and stop background music
        }

        // This method handles window resizing to maintain the aspect ratio (16:9)
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Prevent recursion during resizing
            if (_resizing) return;

            _resizing = true;

            // Adjust width or height to maintain aspect ratio
            if (e.WidthChanged)
            {
                this.Height = this.Width / AspectRatio; // Adjust height based on width
            }
            else if (e.HeightChanged)
            {
                this.Width = this.Height * AspectRatio; // Adjust width based on height
            }

            _resizing = false;
        }
    }
}
