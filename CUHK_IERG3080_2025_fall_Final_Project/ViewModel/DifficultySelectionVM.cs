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
        private OnlineSession _session;
        private bool _handlingRemote;

        private Action<SelectDifficultyMsg> _onSelectDifficultyHandler;
        private Action<int> _onConnectedHandler;
        private Action<string> _onDisconnectedHandler;

        public string SongName { get; }
        public string SongCoverPath { get; private set; }
        public string SongDisplayName { get; private set; }

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

        private bool _isP1EasySelected;
        private bool _isP1HardSelected;

        public bool IsP1EasySelected
        {
            get => _isP1EasySelected;
            set
            {
                if (_isP1EasySelected == value) return;
                if (!CanEditP1 && !_handlingRemote) return;

                _isP1EasySelected = value;
                if (value)
                {
                    _isP1HardSelected = false;
                    SetDifficultyInternal(0, "Easy");
                    OnPropertyChanged(nameof(IsP1HardSelected));

                    TrySendLocalDifficulty(slot: 1, difficulty: "Easy");
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
                    SetDifficultyInternal(0, "Hard");
                    OnPropertyChanged(nameof(IsP1EasySelected));

                    TrySendLocalDifficulty(slot: 1, difficulty: "Hard");
                }

                OnPropertyChanged(nameof(IsP1HardSelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        private bool _isP2EasySelected;
        private bool _isP2HardSelected;

        public bool IsP2EasySelected
        {
            get => _isP2EasySelected;
            set
            {
                if (_isP2EasySelected == value) return;
                if (!CanEditP2 && !_handlingRemote) return;

                _isP2EasySelected = value;
                if (value)
                {
                    _isP2HardSelected = false;
                    SetDifficultyInternal(1, "Easy");
                    OnPropertyChanged(nameof(IsP2HardSelected));

                    TrySendLocalDifficulty(slot: 2, difficulty: "Easy");
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
                    SetDifficultyInternal(1, "Hard");
                    OnPropertyChanged(nameof(IsP2EasySelected));

                    TrySendLocalDifficulty(slot: 2, difficulty: "Hard");
                }

                OnPropertyChanged(nameof(IsP2HardSelected));
                OnPropertyChanged(nameof(IsPlayButtonEnabled));
            }
        }

        public bool IsPlayButtonEnabled
        {
            get
            {
                bool p1Selected = IsP1EasySelected || IsP1HardSelected;

                if (IsMultiplayer)
                {
                    bool p2Selected = IsP2EasySelected || IsP2HardSelected;

                    // Online joiner 禁用 Play（只等 host 开始）
                    if (IsOnlineConnected && GameModeManager.OnlineSession != null && !GameModeManager.OnlineSession.IsHost)
                        return false;

                    return p1Selected && p2Selected;
                }

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
            ApplyCachedDifficultiesIfAny();
            RaiseAll();
        }

        private void AttachOnlineIfNeeded()
        {
            if (_session != null) return;
            if (!(GameModeManager.CurrentMode is OnlineMultiPlayerMode)) return;
            if (GameModeManager.OnlineSession == null) return;

            _session = GameModeManager.OnlineSession;

            _onSelectDifficultyHandler = msg =>
            {
                if (msg == null) return;
                if (string.IsNullOrWhiteSpace(msg.Difficulty)) return;

                int idx = msg.Slot - 1;
                if (idx < 0 || idx > 1) return;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _handlingRemote = true;
                    try
                    {
                        ApplyRemoteDifficulty(idx, msg.Difficulty);
                    }
                    finally
                    {
                        _handlingRemote = false;
                    }
                }));
            };

            _onConnectedHandler = _ =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyCachedDifficultiesIfAny();
                    RaiseAll();
                }));
            };

            _onDisconnectedHandler = _ =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(RaiseAll));
            };

            // ✅ 同样：不要用 IsConnected 作为订阅条件
            _session.OnSelectDifficulty += _onSelectDifficultyHandler;
            _session.OnConnected += _onConnectedHandler;
            _session.OnDisconnected += _onDisconnectedHandler;

            OnPropertyChanged(nameof(CanEditP1));
            OnPropertyChanged(nameof(CanEditP2));
            OnPropertyChanged(nameof(IsPlayButtonEnabled));
        }

        private void ApplyCachedDifficultiesIfAny()
        {
            var s = _session;
            if (s == null) return;

            if (!string.IsNullOrWhiteSpace(s.Slot1Difficulty))
                ApplyRemoteDifficulty(0, s.Slot1Difficulty);

            if (!string.IsNullOrWhiteSpace(s.Slot2Difficulty))
                ApplyRemoteDifficulty(1, s.Slot2Difficulty);
        }

        private void TrySendLocalDifficulty(int slot, string difficulty)
        {
            if (!IsOnlineConnected) return;
            if (_session == null || !_session.IsConnected) return;
            if (_handlingRemote) return;

            // 只让“本机控制的玩家”发
            if (slot == 1 && !CanEditP1) return;
            if (slot == 2 && !CanEditP2) return;

            try
            {
                _ = _session.SendSelectDifficultyAsync(slot, difficulty);
            }
            catch { }
        }

        private void ApplyRemoteDifficulty(int playerIndex, string difficulty)
        {
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

            if (players[playerIndex]?.Difficulty != null)
                players[playerIndex].Difficulty.CurrentDifficulty = difficulty;
        }

        public void Dispose()
        {
            if (_session != null)
            {
                if (_onSelectDifficultyHandler != null) _session.OnSelectDifficulty -= _onSelectDifficultyHandler;
                if (_onConnectedHandler != null) _session.OnConnected -= _onConnectedHandler;
                if (_onDisconnectedHandler != null) _session.OnDisconnected -= _onDisconnectedHandler;
            }

            _onSelectDifficultyHandler = null;
            _onConnectedHandler = null;
            _onDisconnectedHandler = null;
            _session = null;
        }
    }
}
