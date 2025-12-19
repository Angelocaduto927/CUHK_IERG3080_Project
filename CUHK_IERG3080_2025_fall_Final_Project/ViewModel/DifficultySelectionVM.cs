using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class DifficultySelectionVM : ViewModelBase, IDisposable
    {
        // ===== Online sync fields =====
        private OnlineSession _session;
        private Action<SelectSongMsg> _onSelectSongHandler;
        private bool _handlingRemote;

        // 歌曲信息
        public string SongName { get; }
        public string SongCoverPath { get; private set; }
        public string SongDisplayName { get; private set; }

        // 是否多人模式（LocalMulti / OnlineMulti 都算）
        public bool IsMultiplayer { get; }

        private bool IsOnlineConnected =>
            (GameModeManager.CurrentMode is OnlineMultiPlayerMode)
            && GameModeManager.OnlineSession != null
            && GameModeManager.OnlineSession.IsConnected;

        private int LocalSlot =>
            GameModeManager.OnlineSession != null ? GameModeManager.OnlineSession.LocalSlot : 0;

        // 在线：每台机器只能改自己那一侧（Host=Slot1 控 P1；Joiner=Slot2 控 P2）
        public bool CanEditP1 => !IsOnlineConnected || LocalSlot == 1;
        public bool CanEditP2 => !IsOnlineConnected || LocalSlot == 2;

        // ===== Player 1 selection =====
        private bool _isP1EasySelected;
        private bool _isP1HardSelected;

        public bool IsP1EasySelected
        {
            get => _isP1EasySelected;
            set
            {
                if (_isP1EasySelected == value) return;

                // joiner 不允许点 P1（除非是网络同步触发）
                if (!CanEditP1 && !_handlingRemote) return;

                _isP1EasySelected = value;

                if (value)
                {
                    _isP1HardSelected = false;
                    SetDifficultyInternal(playerIndex: 0, difficulty: "Easy");
                    OnPropertyChanged(nameof(IsP1HardSelected));

                    // 只有“本机允许控制的玩家”才发消息
                    TrySendLocalDifficulty("Easy");
                }

                OnPropertyChanged(nameof(IsP1EasySelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        public bool IsP1HardSelected
        {
            get => _isP1HardSelected;
            set
            {
                if (_isP1HardSelected == value) return;

                if (!CanEditP1 && !_handlingRemote) return;

                _isP1HardSelected = value;

                if (value)
                {
                    _isP1EasySelected = false;
                    SetDifficultyInternal(playerIndex: 0, difficulty: "Hard");
                    OnPropertyChanged(nameof(IsP1EasySelected));

                    TrySendLocalDifficulty("Hard");
                }

                OnPropertyChanged(nameof(IsP1HardSelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        // ===== Player 2 selection =====
        private bool _isP2EasySelected;
        private bool _isP2HardSelected;

        public bool IsP2EasySelected
        {
            get => _isP2EasySelected;
            set
            {
                if (_isP2EasySelected == value) return;

                // host 不允许点 P2（除非是网络同步触发）
                if (!CanEditP2 && !_handlingRemote) return;

                _isP2EasySelected = value;

                if (value)
                {
                    _isP2HardSelected = false;
                    SetDifficultyInternal(playerIndex: 1, difficulty: "Easy");
                    OnPropertyChanged(nameof(IsP2HardSelected));

                    TrySendLocalDifficulty("Easy");
                }

                OnPropertyChanged(nameof(IsP2EasySelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        public bool IsP2HardSelected
        {
            get => _isP2HardSelected;
            set
            {
                if (_isP2HardSelected == value) return;

                if (!CanEditP2 && !_handlingRemote) return;

                _isP2HardSelected = value;

                if (value)
                {
                    _isP2EasySelected = false;
                    SetDifficultyInternal(playerIndex: 1, difficulty: "Hard");
                    OnPropertyChanged(nameof(IsP2EasySelected));

                    TrySendLocalDifficulty("Hard");
                }

                OnPropertyChanged(nameof(IsP2HardSelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        // ===== Play button =====
        public bool IsPlayButtonEnabled
        {
            get
            {
                bool p1Selected = IsP1EasySelected || IsP1HardSelected;

                if (IsMultiplayer)
                {
                    bool p2Selected = IsP2EasySelected || IsP2HardSelected;

                    // ✅ Online joiner 禁用 Play（只等 host 开始）
                    if (IsOnlineConnected && GameModeManager.OnlineSession != null && !GameModeManager.OnlineSession.IsHost)
                        return false;

                    return p1Selected && p2Selected;
                }

                // 单人：如果是 online（理论不会出现），同样禁 joiner
                if (IsOnlineConnected && GameModeManager.OnlineSession != null && !GameModeManager.OnlineSession.IsHost)
                    return false;

                return p1Selected;
            }
        }

        public DifficultySelectionVM(string songName)
        {
            SongName = songName;

            IsMultiplayer = GameModeManager.CurrentMode != null
                            && !string.IsNullOrWhiteSpace(GameModeManager.CurrentMode.ModeName)
                            && GameModeManager.CurrentMode.ModeName.Contains("Multi");

            SetSongInfo();

            AttachOnlineIfNeeded();
            LoadFromModel();
            RaiseAll();
        }

        private void AttachOnlineIfNeeded()
        {
            if (_session != null) return;
            if (!(GameModeManager.CurrentMode is OnlineMultiPlayerMode)) return;
            if (GameModeManager.OnlineSession == null) return;

            _session = GameModeManager.OnlineSession;
            if (!_session.IsConnected) return;

            _onSelectSongHandler = msg =>
            {
                if (msg == null) return;
                if (!string.Equals(msg.SongId, SongName, StringComparison.OrdinalIgnoreCase)) return;
                if (string.IsNullOrWhiteSpace(msg.Difficulty)) return;

                // 关键：通过“我是不是 Host”来推断这个消息是谁发来的
                // - Joiner 收到的一定是 Host 发的 => 更新 P1（index 0）
                // - Host 收到的一定是 Joiner 发的 => 更新 P2（index 1）
                int targetIndex = _session.IsHost ? 1 : 0;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _handlingRemote = true;
                    try
                    {
                        ApplyRemoteDifficulty(targetIndex, msg.Difficulty);
                    }
                    finally
                    {
                        _handlingRemote = false;
                    }
                }));
            };

            _session.OnSelectSong += _onSelectSongHandler;

            // 连接状态变化会影响 CanEdit / Play
            OnPropertyChanged(nameof(CanEditP1));
            OnPropertyChanged(nameof(CanEditP2));
            OnPropertyChanged(nameof(IsPlayButtonEnabled));
        }

        private void TrySendLocalDifficulty(string difficulty)
        {
            if (!IsOnlineConnected) return;
            if (_session == null || !_session.IsConnected) return;
            if (_handlingRemote) return;

            // 只让“本机控制的玩家”发
            if (LocalSlot == 1 && !CanEditP1) return;
            if (LocalSlot == 2 && !CanEditP2) return;

            try
            {
                // 复用 SelectSongMsg：SongId=当前歌，Difficulty=本玩家难度
                _ = _session.SendSelectSongAsync(SongName, difficulty); // fire-and-forget
            }
            catch
            {
                // 不要让 UI 崩
            }
        }

        private void ApplyRemoteDifficulty(int playerIndex, string difficulty)
        {
            // 更新 UI 选中状态（不要走 setter，避免再发回去）
            if (playerIndex == 0)
            {
                _isP1EasySelected = string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase);
                _isP1HardSelected = string.Equals(difficulty, "Hard", StringComparison.OrdinalIgnoreCase);

                OnPropertyChanged(nameof(IsP1EasySelected));
                OnPropertyChanged(nameof(IsP1HardSelected));
            }
            else if (playerIndex == 1)
            {
                _isP2EasySelected = string.Equals(difficulty, "Easy", StringComparison.OrdinalIgnoreCase);
                _isP2HardSelected = string.Equals(difficulty, "Hard", StringComparison.OrdinalIgnoreCase);

                OnPropertyChanged(nameof(IsP2EasySelected));
                OnPropertyChanged(nameof(IsP2HardSelected));
            }

            // 同步到模型（后面 Initialize() 会用 Difficulty 去加载谱）
            SetDifficultyInternal(playerIndex, difficulty);

            OnPropertyChanged(nameof(IsPlayButtonEnabled));
        }

        private void SetSongInfo()
        {
            switch (SongName)
            {
                case "One_Last_Kiss":
                    SongCoverPath = "/Assets/Song/One_Last_Kiss_Cover.png";
                    SongDisplayName = "One Last Kiss";
                    break;
                case "IRIS_OUT":
                    SongCoverPath = "/Assets/Song/IRIS_OUT_Cover.png";
                    SongDisplayName = "Iris Out";
                    break;
                default:
                    SongCoverPath = "/Assets/Image/placeholder.png";
                    SongDisplayName = SongName;
                    break;
            }

            OnPropertyChanged(nameof(SongCoverPath));
            OnPropertyChanged(nameof(SongDisplayName));
        }

        private void LoadFromModel()
        {
            var players = GetCurrentModePlayers();
            if (players == null || players.Count == 0) return;

            string d1 = players.ElementAtOrDefault(0)?.Difficulty?.CurrentDifficulty;
            string d2 = players.ElementAtOrDefault(1)?.Difficulty?.CurrentDifficulty;

            _isP1EasySelected = string.Equals(d1, "Easy", StringComparison.OrdinalIgnoreCase);
            _isP1HardSelected = string.Equals(d1, "Hard", StringComparison.OrdinalIgnoreCase);

            _isP2EasySelected = string.Equals(d2, "Easy", StringComparison.OrdinalIgnoreCase);
            _isP2HardSelected = string.Equals(d2, "Hard", StringComparison.OrdinalIgnoreCase);
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsP1EasySelected));
            OnPropertyChanged(nameof(IsP1HardSelected));
            OnPropertyChanged(nameof(IsP2EasySelected));
            OnPropertyChanged(nameof(IsP2HardSelected));
            OnPropertyChanged(nameof(IsPlayButtonEnabled));
            OnPropertyChanged(nameof(CanEditP1));
            OnPropertyChanged(nameof(CanEditP2));
        }

        private List<PlayerManager> GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            if (mode == null) return null;

            var field = mode.GetType().GetField("_players");
            return field?.GetValue(mode) as List<PlayerManager>;
        }

        private void SetDifficultyInternal(int playerIndex, string difficulty)
        {
            var players = GetCurrentModePlayers();
            if (players == null) return;
            if (playerIndex < 0 || playerIndex >= players.Count) return;

            players[playerIndex].Difficulty.CurrentDifficulty = difficulty;
        }

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
