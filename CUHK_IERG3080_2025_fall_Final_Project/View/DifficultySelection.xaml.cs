using System;
using System.Windows;
using System.Windows.Controls;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    public partial class DifficultySelection : UserControl
    {
        public DifficultySelection()
        {
            InitializeComponent();
            Unloaded += DifficultySelection_Unloaded;
        }

        private void DifficultySelection_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
