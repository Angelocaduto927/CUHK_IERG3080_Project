using System.ComponentModel;
using CUHK_IERG3080_2025_fall_Final_Project.Model;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Property for EasyA selection (Song1, Easy)
        public bool IsEasyASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Easy";
            set
            {
                if (value)
                    SetSongAndDifficulty(Hyperparameters.Song1Name, "Easy");
            }
        }

        // Property for HardA selection (Song1, Hard)
        public bool IsHardASelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song1Name && GetCurrentDifficulty() == "Hard";
            set
            {
                if (value)
                    SetSongAndDifficulty(Hyperparameters.Song1Name, "Hard");
            }
        }

        // Property for EasyB selection (Song2, Easy)
        public bool IsEasyBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Easy";
            set
            {
                if (value)
                    SetSongAndDifficulty(Hyperparameters.Song2Name, "Easy");
            }
        }

        // Property for HardB selection (Song2, Hard)
        public bool IsHardBSelected
        {
            get => SongManager.CurrentSong == Hyperparameters.Song2Name && GetCurrentDifficulty() == "Hard";
            set
            {
                if (value)
                    SetSongAndDifficulty(Hyperparameters.Song2Name, "Hard");
            }
        }

        // Property for enabling/disabling the Play button
        public bool IsPlayButtonEnabled
        {
            get
            {
                return (IsEasyASelected || IsHardASelected || IsEasyBSelected || IsHardBSelected);
            }
        }

        public SongSelectionVM()
        {
            LoadFromModel();
        }

        // Directly sets the song and difficulty without any intermediary variable
        private void SetSongAndDifficulty(string song, string difficulty)
        {
            // Set the song
            SongManager.SetSong(song);

            // Set the difficulty for all players
            var players = GetCurrentModePlayers();
            foreach (var player in players)
            {
                player.Difficulty.CurrentDifficulty = difficulty;
            }

            RaiseAll(); // Update UI for all difficulty selections
        }

        // Retrieves the current difficulty of the first player in the game mode
        private string GetCurrentDifficulty()
        {
            var players = GetCurrentModePlayers();
            return players.Count > 0 ? players[0]?.Difficulty?.CurrentDifficulty : null;
        }

        // Gets the list of players from the current game mode
        private System.Collections.Generic.List<PlayerManager> GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            var field = mode.GetType().GetField("_players");
            return field?.GetValue(mode) as System.Collections.Generic.List<PlayerManager>;
        }

        // Initializes the view model based on the current game mode and selected song
        private void LoadFromModel()
        {
            var players = GetCurrentModePlayers();
            if (players == null || players.Count == 0) return;

            string currentSong = SongManager.CurrentSong;
            string currentDifficulty = players[0]?.Difficulty?.CurrentDifficulty;

            // Set the button states based on the current song and difficulty
            if (!string.IsNullOrEmpty(currentSong) && !string.IsNullOrEmpty(currentDifficulty))
            {
                if (currentSong == "Song1" && currentDifficulty == "Easy") IsEasyASelected = true;
                else if (currentSong == "Song1" && currentDifficulty == "Hard") IsHardASelected = true;
                else if (currentSong == "Song2" && currentDifficulty == "Easy") IsEasyBSelected = true;
                else if (currentSong == "Song2" && currentDifficulty == "Hard") IsHardBSelected = true;
            }
        }

        // Updates the UI for all selection buttons
        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsEasyASelected));
            OnPropertyChanged(nameof(IsHardASelected));
            OnPropertyChanged(nameof(IsEasyBSelected));
            OnPropertyChanged(nameof(IsHardBSelected));
            OnPropertyChanged(nameof(IsPlayButtonEnabled));  // Raise PropertyChanged for PlayButtonEnabled
        }

        // Notifies that a property has changed
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
