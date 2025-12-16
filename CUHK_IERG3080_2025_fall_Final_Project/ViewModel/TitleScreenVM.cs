using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class TitleScreenVM : ViewModelBase
    {
        public ICommand SinglePlayerCommand { get; }
        public ICommand LocalMultiPlayerCommand { get; }
        public ICommand OnlineMultiPlayerCommand { get; }

        public TitleScreenVM()
        {
            SinglePlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.SinglePlayer);
            });

            LocalMultiPlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.LocalMultiPlayer);
            });

            OnlineMultiPlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.OnlineMultiPlayer);
            });
        }  

        private void SetMode(GameModeManager.Mode mode)
        {
            // Set the game mode
            GameModeManager.SetMode(mode);

            // Initialize the mode
            if (GameModeManager.CurrentMode != null)
            {
                GameModeManager.CurrentMode.Initialize();
            }
        }
    }
}