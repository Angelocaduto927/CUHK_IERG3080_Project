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

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    /// <summary>
    /// TitleScreen.xaml 的交互逻辑
    /// </summary>
    public partial class TitleScreen : UserControl
    {
        public TitleScreen()
        {
            InitializeComponent();
            AudioManager.PlayBackgroundMusic();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
