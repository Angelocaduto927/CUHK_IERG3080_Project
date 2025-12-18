using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    public class InGameVM : INotifyPropertyChanged
    {
        private GameEngine _engine;
        private DispatcherTimer _timer;
        private bool _initialized = false;
        private Action _onGameOver;
        private MusicManager _musicManager;

        // Note collections for rendering
        public ObservableCollection<NoteVM> Player1Notes { get; } = new ObservableCollection<NoteVM>();
        public ObservableCollection<NoteVM> Player2Notes { get; } = new ObservableCollection<NoteVM>();

        // Game info
        public string SongName => SongManager.CurrentSong ?? "Unknown Song";
        public double CurrentTime => _engine?.CurrentTime / 1000.0 ?? 0;
        public double SongDuration => 180.0; // Default 3 minutes, could be dynamic
        public bool IsMultiplayer => _engine?.Players?.Count > 1;

        // Player 1 Properties
        public string P1Name
        {
            get
            {
                if (_engine?.Players != null && _engine.Players.Count > 0)
                    return _engine.Players[0].PlayerName;
                return "Player 1";
            }
        }
        public int P1Score => _engine?.Players != null && _engine.Players.Count > 0 ? _engine.Players[0].Score.Score : 0;
        public int P1Combo => _engine?.Players != null && _engine.Players.Count > 0 ? _engine.Players[0].Score.Combo : 0;
        public double P1Accuracy => _engine?.Players != null && _engine.Players.Count > 0 ? _engine.Players[0].Score.Accuracy : 100;

        // Player 2 Properties
        public string P2Name => _engine?.Players != null && _engine.Players.Count > 1 ? _engine.Players[1].PlayerName : "Player 2";
        public int P2Score => _engine?.Players != null && _engine.Players.Count > 1 ? _engine.Players[1].Score.Score : 0;
        public int P2Combo => _engine?.Players != null && _engine.Players.Count > 1 ? _engine.Players[1].Score.Combo : 0;
        public double P2Accuracy => _engine?.Players != null && _engine.Players.Count > 1 ? _engine.Players[1].Score.Accuracy : 100;

        public InGameVM() : this(null) { }

        public InGameVM(Action onGameOver)
        {
            _onGameOver = onGameOver;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
            _timer.Tick += (s, e) => Update();

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var window = Application.Current.MainWindow;
                if (window != null)
                {
                    window.Focus();
                    window.PreviewKeyDown += OnKeyDown;
                }

                // Initialize game mode
                if (GameModeManager.CurrentMode != null)
                {
                    GameModeManager.CurrentMode.Initialize();

                    // Get players from game mode using reflection
                    var playersField = GameModeManager.CurrentMode.GetType().GetField("_players",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var players = playersField?.GetValue(GameModeManager.CurrentMode) as System.Collections.Generic.List<PlayerManager>;

                    if (players != null && players.Count > 0)
                    {
                        _engine = new GameEngine();
                        _musicManager = new MusicManager();
                        _engine.Initialize(players);
                        _engine.StartGame();

                        // Start background music
                        AudioManager.StopBackgroundMusic();
                        _musicManager.Play(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Song", $"{SongManager.CurrentSong}.mp3"));
                        _timer.Start();
                        
                        OnPropertyChanged(""); // Notify all properties
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Find which player and which note type
            for (int i = 0; i < _engine.Players.Count; i++)
            {
                var settings = PlayerSettingsManager.GetSettings(i);
                if (settings.KeyBindings.ContainsValue(key))
                {
                    _engine.HandleKeyPress(i, key);
                    e.Handled = true;
                    break;
                }
            }
        }

        private void Update()
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            // Update game engine (spawns notes, checks misses, updates positions)
            _engine.Update();

            // Update visual note collections
            UpdateNotes(0, Player1Notes);
            if (IsMultiplayer)
            {
                UpdateNotes(1, Player2Notes);
            }

            // Update all UI properties
            OnPropertyChanged(nameof(CurrentTime));
            OnPropertyChanged(nameof(P1Score));
            OnPropertyChanged(nameof(P1Combo));
            OnPropertyChanged(nameof(P1Accuracy));

            if (IsMultiplayer)
            {
                OnPropertyChanged(nameof(P2Score));
                OnPropertyChanged(nameof(P2Combo));
                OnPropertyChanged(nameof(P2Accuracy));
            }

            // Check if game is finished
            if (_engine.IsGameFinished())
            {
                _timer.Stop();
                _engine.StopGame();
                AudioManager.StopBackgroundMusic();
                _onGameOver?.Invoke();
            }
        }

        private void UpdateNotes(int playerIdx, ObservableCollection<NoteVM> noteCollection)
        {
            if (_engine?.Players == null || playerIdx >= _engine.Players.Count) return;

            var player = _engine.Players[playerIdx];
            if (player.Chart == null) return;

            // Get all active notes
            var activeNotes = player.Chart
                .Where(n => n.State == Note.NoteState.Active)
                .ToList();

            // Clear and rebuild collection
            noteCollection.Clear();

            foreach (var note in activeNotes)
            {
                // Calculate Y position based on player index
                double yPos = playerIdx == 0 ? 50 : 50; // Both centered vertically in their lanes

                noteCollection.Add(new NoteVM
                {
                    X = note.X - 30, // Center the 60px note
                    Y = yPos,
                    Inner = note.Type == Note.NoteType.Red
                        ? Color.FromRgb(255, 120, 120)
                        : Color.FromRgb(120, 170, 255),
                    Outer = note.Type == Note.NoteType.Red
                        ? Color.FromRgb(180, 0, 0)
                        : Color.FromRgb(0, 100, 220),
                    Border = note.Type == Note.NoteType.Red
                        ? Brushes.DarkRed
                        : Brushes.DarkBlue,
                    Icon = note.Type == Note.NoteType.Red ? "ド" : "カ"
                });
            }
        }

        public void Cleanup()
        {
            _timer?.Stop();

            var window = Application.Current.MainWindow;
            if (window != null)
            {
                window.PreviewKeyDown -= OnKeyDown;
            }

            AudioManager.StopBackgroundMusic();
            _engine?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NoteVM
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Color Inner { get; set; }
        public Color Outer { get; set; }
        public Brush Border { get; set; }
        public string Icon { get; set; }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}