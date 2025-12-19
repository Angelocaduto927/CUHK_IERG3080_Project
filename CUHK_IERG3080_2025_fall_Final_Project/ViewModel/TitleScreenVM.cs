using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Windows;
using System.Windows.Input;
using CUHK_IERG3080_2025_fall_Final_Project.View;

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

                var lobby = new LobbyWindow();
                lobby.Owner = Application.Current.MainWindow;

                bool? ok = lobby.ShowDialog();
                if (ok == true)
                {
                    // ★关键：把已经连接好的 Session 保存到全局
                    GameModeManager.OnlineSession = lobby.VM.Session;

                    _navigateToSongSelection?.Invoke();
                }
                else
                {
                    // 可选：取消就清掉，避免残留
                    GameModeManager.OnlineSession = null;
                }
            });

        }

        private void SetMode(GameModeManager.Mode mode)
        {
            GameModeManager.SetMode(mode);
        }
    }
}
