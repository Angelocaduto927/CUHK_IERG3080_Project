using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;
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
using System.Diagnostics;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    /// <summary>
    /// SongSelection.xaml 的交互逻辑
    /// </summary>
    public partial class SongSelection : UserControl
    {
        public SongSelection()
        {
            InitializeComponent();
            AudioManager.PlayBackgroundMusic();
        }

        private void OnSong1Click(object sender, MouseButtonEventArgs e)
        {
            var navVM = Application.Current.MainWindow?.DataContext as NavigationVM;
            if (navVM != null)
            {
                navVM.DifficultySelectionCommand.Execute("One_Last_Kiss");
           }
        }

        private void OnSong2Click(object sender, MouseButtonEventArgs e)
        {
            var navVM = Application.Current.MainWindow?.DataContext as NavigationVM;
            if (navVM != null)
            {
                navVM.DifficultySelectionCommand.Execute("IRIS_OUT");
            }
        }
    }
}
