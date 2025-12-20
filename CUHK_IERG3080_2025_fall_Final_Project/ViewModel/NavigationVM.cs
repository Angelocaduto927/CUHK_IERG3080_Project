using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Windows;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    class NavigationVM : Utility.ViewModelBase
    {
        private OnlineSession _hookedSession;

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
            CurrentViewModel = new SettingVM();
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
            }

            _hookedSession = s;
            _hookedSession.OnStart += OnOnlineStart;
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
