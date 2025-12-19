using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class DifficultySelectionVM : ViewModelBase
    {
        private readonly Action _navigateToSongSelection;
        private readonly Action _navigateToInGame;

        // 歌曲信息
        public string SongName { get; }
        public string SongCoverPath { get; set; }
        public string SongDisplayName { get; set; }

        // 是否多人模式
        public bool IsMultiplayer { get; }

        // Player 1 难度选择
        private bool _isP1EasySelected;
        private bool _isP1HardSelected;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsP1EasySelected
        {
            get => _isP1EasySelected;
            set
            {
                if (_isP1EasySelected != value)
                {
                    _isP1EasySelected = value;
                    if (value)
                    {
                        _isP1HardSelected = false;
                        UpdatePlayerDifficulty(0, "Easy");
                        OnPropertyChanged(nameof(IsP1HardSelected));
                    }
                    OnPropertyChanged(nameof(IsP1EasySelected));
                    OnPropertyChanged(nameof(IsPlayButtonEnabled));
                }
            }
        }

        public bool IsP1HardSelected
        {
            get => _isP1HardSelected;
            set
            {
                if (_isP1HardSelected != value)
                {
                    _isP1HardSelected = value;
                    if (value)
                    {
                        _isP1EasySelected = false;
                        UpdatePlayerDifficulty(0, "Hard");
                        OnPropertyChanged(nameof(IsP1EasySelected));
                    }
                    OnPropertyChanged(nameof(IsP1HardSelected));
                    OnPropertyChanged(nameof(IsPlayButtonEnabled));
                }
            }
        }

        // ✅ Player 2 难度选择（仅多人模式）
        private bool _isP2EasySelected;
        private bool _isP2HardSelected;

        public bool IsP2EasySelected
        {
            get => _isP2EasySelected;
            set
            {
                if (_isP2EasySelected != value)
                {
                    _isP2EasySelected = value;
                    if (value)
                    {
                        _isP2HardSelected = false;
                        UpdatePlayerDifficulty(1, "Easy");
                        OnPropertyChanged(nameof(IsP2HardSelected));
                    }
                    OnPropertyChanged(nameof(IsP2EasySelected));
                    OnPropertyChanged(nameof(IsPlayButtonEnabled));
                }
            }
        }

        public bool IsP2HardSelected
        {
            get => _isP2HardSelected;
            set
            {
                if (_isP2HardSelected != value)
                {
                    _isP2HardSelected = value;
                    if (value)
                    {
                        _isP2EasySelected = false;
                        UpdatePlayerDifficulty(1, "Hard");
                        OnPropertyChanged(nameof(IsP2EasySelected));
                    }
                    OnPropertyChanged(nameof(IsP2HardSelected));
                    OnPropertyChanged(nameof(IsPlayButtonEnabled));
                }
            }
        }

        // ✅ Play 按钮启用条件
        public bool IsPlayButtonEnabled
        {
            get
            {
                bool p1Selected = IsP1EasySelected || IsP1HardSelected;

                if (IsMultiplayer)
                {
                    bool p2Selected = IsP2EasySelected || IsP2HardSelected;
                    return p1Selected && p2Selected;
                }

                return p1Selected;
            }
        }

        // Gets the list of players from the current game mode
        private System.Collections.Generic.List<PlayerManager> GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            var field = mode.GetType().GetField("_players");
            return field?.GetValue(mode) as System.Collections.Generic.List<PlayerManager>;
        }

        // Initializes the view model based on the current game mode and selected song


        public DifficultySelectionVM(string songName)
        {

            SongName = songName;

            // 检查是否多人模式
            IsMultiplayer = GameModeManager.CurrentMode != null &&
                           GameModeManager.CurrentMode.ModeName.Contains("Multi");

            // 设置歌曲信息
            SetSongInfo();

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
        }

        private void UpdatePlayerDifficulty(int playerIndex, string difficulty)
        {
            if (GameModeManager.CurrentMode != null)
            {
                var playersField = GameModeManager.CurrentMode.GetType().GetField("_players");
                if (playersField != null)
                {
                    var players = playersField.GetValue(GameModeManager.CurrentMode)
                        as System.Collections.Generic.List<PlayerManager>;

                    if (players != null && playerIndex < players.Count)
                    {
                        players[playerIndex].Difficulty.CurrentDifficulty = difficulty;
                    }
                }
            }
        }
    }
}