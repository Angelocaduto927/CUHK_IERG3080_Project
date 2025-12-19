using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System.Windows.Input;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System.Windows;




namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    class NavigationVM : Utility.ViewModelBase
    {

        private OnlineSession _hookedSession;
        private bool _p1Ready;
        private bool _p2Ready;
        private bool _startSent;


        private object _currentViewModel;
        public object CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();

                // Handle background music based on current view  
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
            //// Ensure a game mode is selected before going to song selection  
            //if (GameModeManager.CurrentMode == null)
            //{
            //    // If no mode selected, go back to title screen  
            //    TitleScreen(null);
            //    return;
            //}

            CurrentViewModel = new SongSelectionVM();
        }

        private void DifficultySelection(object obj)
        {
            string songName = obj as string;
            if (string.IsNullOrEmpty(songName))
            {
                return;
            }
            SongManager.SetSong(songName);
            CurrentViewModel = new DifficultySelectionVM(songName);
            EnsureOnlineHooks();

        }

        private void Setting(object obj)
        {
            // Recreate SettingVM each time to reload current speeds from model  
            CurrentViewModel = new SettingVM();
        }

        private async void InGame(object obj)
        {
            if (GameModeManager.CurrentMode is OnlineMultiPlayerMode
                && GameModeManager.OnlineSession != null
                && GameModeManager.OnlineSession.IsConnected)
            {
                EnsureOnlineHooks();
                var s = GameModeManager.OnlineSession;

                // Host：点开始就发 Start
                if (s.IsHost)
                {
                    CurrentViewModel = new InGameVM(() => GameOver(null));
                    await s.SendStartAsync(1500);
                    return;
                }

                // Joiner：如果你仍然禁用了 Joiner 的 Play，这里其实用不到；
                // 保险起见：允许 joiner 点了也进入等待页（真正开始由 StartMsg 决定）
                CurrentViewModel = new InGameVM(() => GameOver(null));
                return;
            }

            CurrentViewModel = new InGameVM(() => GameOver(null));
        }



        private void GameOver(object obj)
        {
            CurrentViewModel = new GameOverVM(() => TitleScreen(null));
        }

        private void HandleBackgroundMusic(object viewModel)
        {
            // Stop background music when entering InGame view  
            if (viewModel is InGameVM)
            {
                CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.StopBackgroundMusic();
            }
            // Resume/play background music for all other views  
            else
            {
                CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.PlayBackgroundMusic();
            }
        }

        public NavigationVM()
        {
            TitleScreenCommand = new RelayCommand(TitleScreen);
            SongSelectionCommand = new RelayCommand(SongSelection);
            SettingCommand = new RelayCommand(Setting);
            InGameCommand = new RelayCommand(InGame);
            GameOverCommand = new RelayCommand(GameOver);
            DifficultySelectionCommand = new RelayCommand(DifficultySelection);

            // Initialize audio system  
            CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.Initialize();

            // Start with title screen and background music  
            CurrentViewModel = new TitleScreenVM(() => SongSelection(null));
            CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.PlayBackgroundMusic();
        }

        private void EnsureOnlineHooks()
        {
            var s = GameModeManager.OnlineSession;
            if (s == null || !s.IsConnected) return;

            if (_hookedSession == s) return;

            // 换 session 的时候卸载旧的
            if (_hookedSession != null)
            {
                _hookedSession.OnReady -= OnOnlineReady;
                _hookedSession.OnStart -= OnOnlineStart;
            }

            _hookedSession = s;
            _hookedSession.OnReady += OnOnlineReady;
            _hookedSession.OnStart += OnOnlineStart;

            // 每次进入 InGame 前都重置状态（避免上局残留）
            _p1Ready = false;
            _p2Ready = false;
            _startSent = false;
        }

        private async void OnOnlineReady(ReadyMsg msg)
        {
            // 记录 ready 状态（这里不用依赖 session 内部字段，最稳）
            if (msg.Slot == 1) _p1Ready = msg.IsReady;
            if (msg.Slot == 2) _p2Ready = msg.IsReady;

            // 只有 Host 才负责广播 Start
            if (_hookedSession == null || !_hookedSession.IsHost) return;

            if (!_startSent && _p1Ready && _p2Ready)
            {
                _startSent = true;

                // 1.5 秒后同时开始（你可以改 1500）
                await _hookedSession.SendStartAsync(1500);
            }
        }
        private void OnOnlineStart(StartMsg msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentViewModel is InGameVM) return;
                CurrentViewModel = new InGameVM(() => GameOver(null));
            });
        }



    }
}
