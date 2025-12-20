using System;
using System.ComponentModel;
using System.Windows;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OnlineSession _session;
        private bool _handlingRemote;

        private Action<SelectSongMsg> _onSelectSongHandler;
        private Action<int> _onConnectedHandler;
        private Action<string> _onDisconnectedHandler;

        // Joiner 应该不能改选歌 & 不能按 Play（只跟随 Host）
        public bool CanChangeSelection
        {
            get
            {
                var s = _session;
                if (s != null && s.IsConnected && !s.IsHost) return false;
                return true;
            }
        }

        public bool IsEasyASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Easy";
            set { if (value) SetSongAndDifficulty(Hyperparameters.Song1Name, "Easy"); }
        }

        public bool IsHardASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Hard";
            set { if (value) SetSongAndDifficulty(Hyperparameters.Song1Name, "Hard"); }
        }

        public bool IsEasyBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Easy";
            set { if (value) SetSongAndDifficulty(Hyperparameters.Song2Name, "Easy"); }
        }

        public bool IsHardBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Hard";
            set { if (value) SetSongAndDifficulty(Hyperparameters.Song2Name, "Hard"); }
        }

        public bool IsPlayButtonEnabled
        {
            get
            {
                var s = _session;
                if (s != null && s.IsConnected && !s.IsHost) return false; // joiner 禁用

                return (IsEasyASelected || IsHardASelected || IsEasyBSelected || IsHardBSelected);
            }
        }

        public SongSelectionVM()
        {
            AttachOnlineIfNeeded();
            ApplyCachedSelectionIfAny();
            RaiseAll();
        }

        private void AttachOnlineIfNeeded()
        {
            if (_session != null) return;

            if (!(GameModeManager.CurrentMode is OnlineMultiPlayerMode)) return;
            if (GameModeManager.OnlineSession == null) return;

            _session = GameModeManager.OnlineSession;

            _onSelectSongHandler = msg =>
            {
                if (msg == null) return;

                // 只有 Joiner 才需要“收到 Host 选择 -> 更新本地”
                if (_session.IsHost) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _handlingRemote = true;
                    try
                    {
                        SetSongAndDifficultyInternal(msg.SongId, msg.Difficulty);
                    }
                    finally
                    {
                        _handlingRemote = false;
                    }
                });
            };

            _onConnectedHandler = _ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyCachedSelectionIfAny();
                    RaiseAll();
                });
            };

            _onDisconnectedHandler = _ =>
            {
                Application.Current.Dispatcher.Invoke(RaiseAll);
            };

            // ✅ 不要用 IsConnected 作为订阅条件（Joiner 可能“进页面时还没 JoinOk”，会导致永远收不到同步）
            _session.OnSelectSong += _onSelectSongHandler;
            _session.OnConnected += _onConnectedHandler;
            _session.OnDisconnected += _onDisconnectedHandler;
        }

        private void ApplyCachedSelectionIfAny()
        {
            var s = _session;
            if (s == null) return;

            var last = s.LastSelectSongMsg;
            if (last == null) return;
            if (string.IsNullOrWhiteSpace(last.SongId)) return;
            if (string.IsNullOrWhiteSpace(last.Difficulty)) return;

            _handlingRemote = true;
            try
            {
                SetSongAndDifficultyInternal(last.SongId, last.Difficulty);
            }
            finally
            {
                _handlingRemote = false;
            }
        }

        private void SetSongAndDifficulty(string song, string difficulty)
        {
            // Online joiner 禁止自己点（只跟随 host）
            if (_session != null && _session.IsConnected && !_session.IsHost && !_handlingRemote)
                return;

            // 1) 本地更新
            SetSongAndDifficultyInternal(song, difficulty);

            // 2) Host 发给 Joiner
            if (_session != null && _session.IsConnected && _session.IsHost && !_handlingRemote)
            {
                try
                {
                    _ = _session.SendSelectSongAsync(song, difficulty);
                }
                catch { }
            }
        }

        private void SetSongAndDifficultyInternal(string song, string difficulty)
        {
            if (!string.IsNullOrWhiteSpace(song))
                SongManager.SetSong(song);

            var players = GetCurrentModePlayers();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player?.Difficulty != null)
                        player.Difficulty.CurrentDifficulty = difficulty;
                }
            }

            RaiseAll();
        }

        private string GetCurrentDifficulty()
        {
            var players = GetCurrentModePlayers();
            return (players != null && players.Count > 0)
                ? players[0]?.Difficulty?.CurrentDifficulty
                : null;
        }

        private System.Collections.Generic.List<PlayerManager> GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            if (mode == null) return null;

            var field = mode.GetType().GetField("_players");
            return field?.GetValue(mode) as System.Collections.Generic.List<PlayerManager>;
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsEasyASelected));
            OnPropertyChanged(nameof(IsHardASelected));
            OnPropertyChanged(nameof(IsEasyBSelected));
            OnPropertyChanged(nameof(IsHardBSelected));
            OnPropertyChanged(nameof(IsPlayButtonEnabled));
            OnPropertyChanged(nameof(CanChangeSelection));
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_session != null)
            {
                if (_onSelectSongHandler != null) _session.OnSelectSong -= _onSelectSongHandler;
                if (_onConnectedHandler != null) _session.OnConnected -= _onConnectedHandler;
                if (_onDisconnectedHandler != null) _session.OnDisconnected -= _onDisconnectedHandler;
            }

            _onSelectSongHandler = null;
            _onConnectedHandler = null;
            _onDisconnectedHandler = null;
            _session = null;
        }
    }
}
