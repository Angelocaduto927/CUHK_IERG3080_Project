using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class TitleScreenVM : ViewModelBase
    {
        private readonly Action _navigateToSongSelection;
        public ICommand SinglePlayerCommand { get; }
        public ICommand LocalMultiPlayerCommand { get; }
        public ICommand OnlineMultiPlayerCommand { get; }

        public TitleScreenVM(Action navigateToSongSelection = null)
        {
            _navigateToSongSelection = navigateToSongSelection;
            SinglePlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.SinglePlayer);
                _navigateToSongSelection?.Invoke();
            });

            LocalMultiPlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.LocalMultiPlayer);
                _navigateToSongSelection?.Invoke();
            });

            OnlineMultiPlayerCommand = new RelayCommand(_ =>
            {
                SetMode(GameModeManager.Mode.OnlineMultiPlayer);
                _navigateToSongSelection?.Invoke();
            });
        }  

        private void SetMode(GameModeManager.Mode mode)
        {
            // Set the game mode
            GameModeManager.SetMode(mode);
            /*
            // Initialize the mode
            if (GameModeManager.CurrentMode != null)
            {
                GameModeManager.CurrentMode.Initialize();
            }
            */
        }
    }
}