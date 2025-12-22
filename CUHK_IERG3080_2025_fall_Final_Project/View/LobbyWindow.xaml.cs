using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;
using System.Windows;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    public partial class LobbyWindow : Window
    {
        public LobbyViewModel VM { get; private set; }

        public LobbyWindow()
        {
            InitializeComponent();

            VM = new LobbyViewModel();
            DataContext = VM;

            VM.RequestClose += ok =>
            {
                DialogResult = ok;
                Close();
            };

            // ✅ 关闭窗口时：如果不是 OK（DialogResult != true），就断开连接
            Closed += (s, e) => VM.OnWindowClosed(DialogResult == true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
