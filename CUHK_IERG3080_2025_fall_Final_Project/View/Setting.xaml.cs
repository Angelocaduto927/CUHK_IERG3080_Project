using System.Windows;
using System.Windows.Controls;
using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    public partial class Setting : UserControl
    {
        public Setting()
        {
            InitializeComponent();
            Loaded += (s, e) => (DataContext as SettingVM)?.AttachWindow(Window.GetWindow(this));
            Unloaded += (s, e) => (DataContext as SettingVM)?.DetachWindow();
        }
    }
}