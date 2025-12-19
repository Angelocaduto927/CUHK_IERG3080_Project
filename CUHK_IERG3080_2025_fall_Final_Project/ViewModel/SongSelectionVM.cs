using System;
using System.ComponentModel;
using System.Windows;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared; // 为了 Action<SelectSongMsg>

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ===== Online sync fields =====
        private OnlineSession _session;
        private Action<SelectSongMsg> _onSelectSongHandler;
        private bool _handlingRemote; // 防止“收到网络更新又反发回去/Joiner误操作”

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

        // Property for EasyA selection (Song1, Easy)
        public bool IsEasyASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Easy";
            set
            {
                if (value) SetSongAndDifficulty(Hyperparameters.Song1Name, "Easy");
            }
        }

        // Property for HardA selection (Song1, Hard)
        public bool IsHardASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Hard";
            set
            {
                if (value) SetSongAndDifficulty(Hyperparameters.Song1Name, "Hard");
            }
        }

        // Property for EasyB selection (Song2, Easy)
        public bool IsEasyBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Easy";
            set
            {
                if (value) SetSongAndDifficulty(Hyperparameters.Song2Name, "Easy");
            }
        }

        // Property for HardB selection (Song2, Hard)
        public bool IsHardBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Hard";
            set
            {
                if (value) SetSongAndDifficulty(Hyperparameters.Song2Name, "Hard");
            }
        }

        // Property for enabling/disabling the Play button
        public bool IsPlayButtonEnabled
        {
            get
            {
                // ✅ Online joiner 禁用 Play（只等 Host 开始）
                var s = _session;
                if (s != null && s.IsConnected && !s.IsHost) return false;

                return (IsEasyASelected || IsHardASelected || IsEasyBSelected || IsHardBSelected);
            }
        }

        public SongSelectionVM()
        {
            LoadFromModel();
            AttachOnlineIfNeeded();
            RaiseAll();
        }

        private void AttachOnlineIfNeeded()
        {
            if (_session != null) return;

            if (!(GameModeManager.CurrentMode is OnlineMultiPlayerMode)) return;
            if (GameModeManager.OnlineSession == null) return;

            _session = GameModeManager.OnlineSession;

            // 没连上就不订阅（避免空 session/重复订阅）
            if (!_session.IsConnected) return;

            _onSelectSongHandler = msg =>
            {
                // Joiner 才响应；Host 自己点的不需要再处理
                if (_session.IsHost) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _handlingRemote = true;
                    try
                    {
                        // ✅ 收到 host 选择：只更新本地，不要再发回去
                        SetSongAndDifficultyInternal(msg.SongId, msg.Difficulty);
                    }
                    finally
                    {
                        _handlingRemote = false;
                    }
                });
            };

            _session.OnSelectSong += _onSelectSongHandler;
        }

        // ====== 你原来的入口：现在加“Host 发送、Joiner 禁止” ======
        private void SetSongAndDifficulty(string song, string difficulty)
        {
            // ✅ Online joiner 禁止自己点（只跟随 host）
            if (_session != null && _session.IsConnected && !_session.IsHost && !_handlingRemote)
            {
                // 你也可以 MessageBox.Show 提示，但建议静默忽略
                return;
            }

            // 1) 先本地更新
            SetSongAndDifficultyInternal(song, difficulty);

            // 2) 再把 host 的选择发给对方
            if (_session != null && _session.IsConnected && _session.IsHost && !_handlingRemote)
            {
                try
                {
                    _ = _session.SendSelectSongAsync(song, difficulty); // fire-and-forget
                }
                catch
                {
                    // 这里可写 Debug/Log，但不要让 UI 崩
                }
            }
        }

        // ✅ 抽出来：只做“本地更新 + RaiseAll”
        private void SetSongAndDifficultyInternal(string song, string difficulty)
        {
            SongManager.SetSong(song);

            var players = GetCurrentModePlayers();
            if (players != null)
            {
                foreach (var player in players)
                    player.Difficulty.CurrentDifficulty = difficulty;
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

        // ✅ 重要：不要在这里 set IsEasyASelected = true（会触发 setter -> 发网络）
        private void LoadFromModel()
        {
            // 你现在这些 IsEasyASelected getter 本来就是根据 SongManager/players 计算的，
            // 所以只要 RaiseAll() 就够了，不要反过来 set 它们。
            RaiseAll();
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

        // ✅ 防止反复进入选歌页导致重复订阅（很重要）
        public void Dispose()
        {
            if (_session != null && _onSelectSongHandler != null)
            {
                _session.OnSelectSong -= _onSelectSongHandler;
            }
            _onSelectSongHandler = null;
            _session = null;
        }
    }
}
