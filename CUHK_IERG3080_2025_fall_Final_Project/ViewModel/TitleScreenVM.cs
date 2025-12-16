using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class TitleScreenVM
    {
        public ICommand SinglePlayerCommand { get; }
        public ICommand LocalMultiPlayerCommand { get; }
        public ICommand OnlineMultiPlayerCommand { get; }

        public TitleScreenVM()
        {
            SinglePlayerCommand = new RelayCommand(() =>
            {
                GameModeManager.SetMode(GameModeManager.Mode.SinglePlayer);
            });

            LocalMultiPlayerCommand = new RelayCommand(() =>
            {
                GameModeManager.SetMode(GameModeManager.Mode.LocalMultiPlayer);
            });

            OnlineMultiPlayerCommand = new RelayCommand(() =>
            {
                GameModeManager.SetMode(GameModeManager.Mode.OnlineMultiPlayer);
            });
        }
    }
}
