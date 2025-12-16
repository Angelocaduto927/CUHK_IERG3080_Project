using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : ViewModelBase
    {
        // Store the selected difficulty globally (for all cards)
        // Format: "SongName_Difficulty" (e.g., "Song1_Easy", "Song2_Hard")
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
                    // Notify all buttons to reflect the new selection
                    RaiseAll();
                    // Update the model
                    UpdateModel();
                }
            }
        }

        /* -------- Card A (Song1) -------- */
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

        /* -------- Card B (Song2) -------- */
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
            // Load current selection from model if available
            LoadFromModel();
        }

        private void LoadFromModel()
        {
            // Try to load existing song and difficulty from current mode
            if (GameModeManager.CurrentMode != null &&
                GameModeManager.CurrentMode.Players != null &&
                GameModeManager.CurrentMode.Players.Count > 0)
            {
                var player = GameModeManager.CurrentMode.Players[0];
                string currentSong = SongManager.CurrentSong;
                string currentDifficulty = player.Difficulty.CurrentDifficulty;

                if (!string.IsNullOrEmpty(currentSong) && !string.IsNullOrEmpty(currentDifficulty))
                {
                    // Reconstruct the selection string
                    _selectedDifficulty = $"{currentSong}_{currentDifficulty}";
                    RaiseAll();
                }
            }
        }

        private void UpdateModel()
        {
            if (string.IsNullOrEmpty(_selectedDifficulty))
                return;

            // Parse the selection: "SongName_Difficulty"
            var parts = _selectedDifficulty.Split('_');
            if (parts.Length != 2)
                return;

            string songName = parts[0];      // "Song1" or "Song2"
            string difficulty = parts[1];    // "Easy" or "Hard"

            // Update SongManager with selected song
            SongManager.SetSong(songName);

            // Update all players in current mode with the selected difficulty
            if (GameModeManager.CurrentMode != null && GameModeManager.CurrentMode.Players != null)
            {
                foreach (var player in GameModeManager.CurrentMode.Players)
                {
                    // Directly set the CurrentDifficulty property (no need for SetDifficulty method)
                    player.Difficulty.CurrentDifficulty = difficulty;
                }
            }
        }

        /* -------- Notify all buttons -------- */
        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsEasyASelected));
            OnPropertyChanged(nameof(IsHardASelected));
            OnPropertyChanged(nameof(IsEasyBSelected));
            OnPropertyChanged(nameof(IsHardBSelected));
        }
    }
}