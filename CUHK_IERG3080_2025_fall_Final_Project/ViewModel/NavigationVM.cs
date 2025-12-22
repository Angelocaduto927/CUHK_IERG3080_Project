using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    class NavigationVM : Utility.ViewModelBase
    {
        private OnlineSession _hookedSession;
        private int _disconnectPopupOnce = 0;

        private object _currentViewModel;
        public object CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                if (ReferenceEquals(_currentViewModel, value)) return;

                // ✅ 切页前 Dispose 旧 VM
                try
                {
                    if (_currentViewModel is IDisposable d)
                        d.Dispose();
                }
                catch { }

                _currentViewModel = value;
                OnPropertyChanged();

                HandleBackgroundMusic(value);
            }
        }

        public ICommand TitleScreenCommand { get; set; }
        public ICommand SongSelectionCommand { get; set; }
        public ICommand SettingCommand { get; set; }
        public ICommand InGameCommand { get; set; }
        public ICommand GameOverCommand { get; set; }
        public ICommand DifficultySelectionCommand { get; set; }

        private void TitleScreen(object obj)
        {
            CurrentViewModel = new TitleScreenVM(() => SongSelection(null));
        }

        private void SongSelection(object obj)
        {
            CurrentViewModel = new SongSelectionVM();
            EnsureOnlineHooks();
        }

        private void DifficultySelection(object obj)
        {
            string songName = obj as string;
            if (string.IsNullOrEmpty(songName)) return;

            SongManager.SetSong(songName);
            CurrentViewModel = new DifficultySelectionVM(songName);

            EnsureOnlineHooks();
        }

        private void Setting(object obj)
        {
            var previous = CurrentViewModel;
            CurrentViewModel = new SettingVM(() =>
            {
                CurrentViewModel = previous;
            });
            EnsureOnlineHooks();
        }

        private async void InGame(object obj)
        {
            if (GameModeManager.CurrentMode is OnlineMultiPlayerMode
                && GameModeManager.OnlineSession != null
                && GameModeManager.OnlineSession.IsConnected)
            {
                EnsureOnlineHooks();
                var s = GameModeManager.OnlineSession;

                CurrentViewModel = new InGameVM(() => GameOver(null));

                if (s.IsHost)
                {
                    try { await s.SendStartAsync(1500); } catch { }
                }
                return;
            }

            CurrentViewModel = new InGameVM(() => GameOver(null));
        }

        private void GameOver(object obj)
        {
            CurrentViewModel = new GameOverVM(() => TitleScreen(null));
            EnsureOnlineHooks();
        }

        private void HandleBackgroundMusic(object viewModel)
        {
            if (viewModel is InGameVM)
                AudioManager.StopBackgroundMusic();
            else
                AudioManager.PlayBackgroundMusic();
        }

        public NavigationVM()
        {
            TitleScreenCommand = new RelayCommand(TitleScreen);
            SongSelectionCommand = new RelayCommand(SongSelection);
            SettingCommand = new RelayCommand(Setting);
            InGameCommand = new RelayCommand(InGame);
            GameOverCommand = new RelayCommand(GameOver);
            DifficultySelectionCommand = new RelayCommand(DifficultySelection);

            AudioManager.Initialize();

            CurrentViewModel = new TitleScreenVM(() => SongSelection(null));
            AudioManager.PlayBackgroundMusic();

            EnsureOnlineHooks();
        }

        private void EnsureOnlineHooks()
        {
            var s = GameModeManager.OnlineSession;
            if (s == null) return;

            if (_hookedSession == s) return;

            if (_hookedSession != null)
            {
                _hookedSession.OnStart -= OnOnlineStart;
                _hookedSession.OnDisconnected -= OnOnlineDisconnected;
            }

            _hookedSession = s;
            Interlocked.Exchange(ref _disconnectPopupOnce, 0);
            _hookedSession.OnStart += OnOnlineStart;
            _hookedSession.OnDisconnected += OnOnlineDisconnected;
        }

        private static bool IsNormalLeaveReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return false;

            // InGameVM 主动 Leave 的 reason（不要弹窗，不要强制回 Title）
            if (reason == "Game finished" || reason == "Music ended" || reason == "Cleanup") return true;
            if (reason == "Leave" || reason == "Dispose" || reason.StartsWith("Restart", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void OnOnlineDisconnected(string reason)
        {
            if (IsNormalLeaveReason(reason)) return;
            if (Interlocked.Exchange(ref _disconnectPopupOnce, 1) == 1) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    MessageBox.Show(
                        "Online connection lost.\nReason: " + (string.IsNullOrWhiteSpace(reason) ? "Disconnected" : reason),
                        "Disconnected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                catch { }

                // ✅ 清理全局 session，避免还在 Online 模式下出各种空引用
                try { GameModeManager.OnlineSession = null; } catch { }
                try { GameModeManager.SetMode(GameModeManager.Mode.SinglePlayer); } catch { }

                // ✅ 解绑事件，避免旧 session 残留触发
                try
                {
                    if (_hookedSession != null)
                    {
                        _hookedSession.OnStart -= OnOnlineStart;
                        _hookedSession.OnDisconnected -= OnOnlineDisconnected;
                        _hookedSession = null;
                    }
                }
                catch { }

                // ✅ 强制回到 TitleScreen
                try { TitleScreen(null); } catch { }
            }));
        }

        private void OnOnlineStart(StartMsg msg)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (CurrentViewModel is InGameVM) return;
                CurrentViewModel = new InGameVM(() => GameOver(null));
            }));
        }
    }
}
