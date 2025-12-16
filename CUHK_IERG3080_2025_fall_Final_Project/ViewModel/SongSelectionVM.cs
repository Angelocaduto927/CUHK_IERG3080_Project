using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : ViewModelBase
    {
        private string _selectedDifficulty;
        public string SelectedDifficulty
        {
            get => _selectedDifficulty;
            set
            {
                if (_selectedDifficulty != value)
                {
                    _selectedDifficulty = value;
                    OnPropertyChanged(nameof(SelectedDifficulty));
                    RaiseAll();
                    UpdateModel();
                }
            }
        }

        public bool IsEasyASelected
        {
            get => SelectedDifficulty == "Song1_Easy";
            set { if (value) SelectedDifficulty = "Song1_Easy"; }
        }

        public bool IsHardASelected
        {
            get => SelectedDifficulty == "Song1_Hard";
            set { if (value) SelectedDifficulty = "Song1_Hard"; }
        }

        public bool IsEasyBSelected
        {
            get => SelectedDifficulty == "Song2_Easy";
            set { if (value) SelectedDifficulty = "Song2_Easy"; }
        }

        public bool IsHardBSelected
        {
            get => SelectedDifficulty == "Song2_Hard";
            set { if (value) SelectedDifficulty = "Song2_Hard"; }
        }

        public SongSelectionVM()
        {
            LoadFromModel();
        }

        private void LoadFromModel()
        {
            var players = GetCurrentModePlayers();
            if (players == null || players.Count == 0)
                return;

            dynamic player = players[0];
            string currentSong = SongManager.CurrentSong;
            string currentDifficulty = player?.Difficulty?.CurrentDifficulty;

            if (!string.IsNullOrEmpty(currentSong) && !string.IsNullOrEmpty(currentDifficulty))
            {
                _selectedDifficulty = $"{currentSong}_{currentDifficulty}";
                RaiseAll();
            }
        }

        private void UpdateModel()
        {
            if (string.IsNullOrEmpty(_selectedDifficulty))
                return;

            var parts = _selectedDifficulty.Split('_');
            if (parts.Length != 2)
                return;

            string songName = parts[0];
            string difficulty = parts[1];

            SongManager.SetSong(songName);

            var players = GetCurrentModePlayers();
            if (players == null)
                return;

            foreach (var playerObj in players)
            {
                dynamic player = playerObj;
                if (player?.Difficulty != null)
                {
                    player.Difficulty.CurrentDifficulty = difficulty;
                }
            }
        }

        private IList GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            if (mode == null)
                return null;

            var property = mode.GetType().GetProperty("Players", BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(mode) as IList;
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsEasyASelected));
            OnPropertyChanged(nameof(IsHardASelected));
            OnPropertyChanged(nameof(IsEasyBSelected));
            OnPropertyChanged(nameof(IsHardBSelected));
        }
    }
}
