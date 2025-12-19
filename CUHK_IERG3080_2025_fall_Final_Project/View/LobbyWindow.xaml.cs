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

            Closed += (s, e) => VM.OnWindowClosed();
        }
    }
}
