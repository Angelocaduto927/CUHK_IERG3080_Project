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

                // ✅ 弹出 LobbyWindow，只有成功连接才进入选歌
                var lobby = new LobbyWindow();
                lobby.Owner = Application.Current.MainWindow;

                bool? ok = lobby.ShowDialog();
                if (ok == true)
                {
                    // 连接成功：进入选歌界面
                    _navigateToSongSelection?.Invoke();
                }
                else
                {
                    // 取消/失败：回到 Title，不跳转
                    // （可选）如果你想：GameModeManager.SetMode(GameModeManager.Mode.SinglePlayer);
                }
            });
        }

        private void SetMode(GameModeManager.Mode mode)
        {
            GameModeManager.SetMode(mode);
        }
    }
}
