using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;

using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    public class InGameVM : INotifyPropertyChanged
    {
        private GameEngine _engine;
        private bool _initialized = false;
        private readonly Action _onGameOver;
        private readonly MusicManager _musicManager;

        // ✅ CHANGED: 用 Rendering 代替 DispatcherTimer
        private bool _renderingAttached = false;
        private double _lastRenderUpdateMs = -9999;  // 用于限帧
        private const double TargetFrameMs = 16.0;   // 60fps

        // Note collections for rendering
        public ObservableCollection<NoteVM> Player1Notes { get; } = new ObservableCollection<NoteVM>();
        public ObservableCollection<NoteVM> Player2Notes { get; } = new ObservableCollection<NoteVM>();

        // Note -> NoteVM reuse maps
        private readonly Dictionary<Note, NoteVM> _p1Map = new Dictionary<Note, NoteVM>();
        private readonly Dictionary<Note, NoteVM> _p2Map = new Dictionary<Note, NoteVM>();

        private int _frameStamp = 0;

        // Game info + Hyperparameters
        public double BandWidth => Hyperparameters.BandWidth;
        public double EllipseS => Hyperparameters.EllipseSize;
        public double BandMid => BandWidth / 2 - 30;
        public double LineDistance => Hyperparameters.LineDistance;
        public double EllipseD => LineDistance + 2 - EllipseS / 2;
        public string Song1Name => Hyperparameters.Song1Name;
        public string Song2Name => Hyperparameters.Song2Name;

        public double P1Coordinate => IsMultiplayer ? Hyperparameters.MultiPlayerUpperYCoordinate : Hyperparameters.SinglePlayerYCoordinate;
        public double P2Coordinate => IsMultiplayer ? Hyperparameters.MultiPlayerLowerYCoordinate : 0;

        public double HitZoneXCoordinate => Hyperparameters.HitZoneXCoordinate;
        public double SpawnZoneXCoordinate => Hyperparameters.SpawnZoneXCoordinate;
        public double DefaultSpeed => Hyperparameters.DefaultSpeed;
        public int DefaultPerfectScore => Hyperparameters.DefaultPerfectScore;
        public int DefaultGoodScore => Hyperparameters.DefaultGoodScore;
        public int DefaultBadScore => Hyperparameters.DefaultBadScore;
        public int DefaultMissScore => Hyperparameters.DefaultMissScore;
        public double PerfectWindow => Hyperparameters.PerfectWindow;
        public double GoodWindow => Hyperparameters.GoodWindow;
        public double BadWindow => Hyperparameters.BadWindow;
        public double MissWindow => Hyperparameters.MissWindow;
        public string SongName => SongManager.CurrentSong ?? "Unknown Song";

        public double CurrentTime => _engine?.CurrentTime / 1000.0 ?? 0;
        public double SongDuration => 180.0;
        public bool IsMultiplayer => _engine?.Players?.Count > 1;

        // Player 1 Properties
        public string P1Name => (_engine?.Players != null && _engine.Players.Count > 0) ? _engine.Players[0].PlayerName : "Player 1";
        public int P1Score => (_engine?.Players != null && _engine.Players.Count > 0) ? _engine.Players[0].Score.Score : 0;
        public int P1Combo => (_engine?.Players != null && _engine.Players.Count > 0) ? _engine.Players[0].Score.Combo : 0;
        public double P1Accuracy => (_engine?.Players != null && _engine.Players.Count > 0) ? _engine.Players[0].Score.Accuracy : 100;

        // Player 2 Properties
        public string P2Name => (_engine?.Players != null && _engine.Players.Count > 1) ? _engine.Players[1].PlayerName : "Player 2";
        public int P2Score => (_engine?.Players != null && _engine.Players.Count > 1) ? _engine.Players[1].Score.Score : 0;
        public int P2Combo => (_engine?.Players != null && _engine.Players.Count > 1) ? _engine.Players[1].Score.Combo : 0;
        public double P2Accuracy => (_engine?.Players != null && _engine.Players.Count > 1) ? _engine.Players[1].Score.Accuracy : 100;

        // --- Hit effect brushes (for hit-circle color flash) ---
        private readonly Brush _p1StrokeDefault;
        private readonly Brush _p1FillDefault;
        private readonly Brush _p2StrokeDefault;
        private readonly Brush _p2FillDefault;

        private Brush _p1EllipseStroke;
        private Brush _p1EllipseFill;
        private Brush _p2EllipseStroke;
        private Brush _p2EllipseFill;

        // --- New: hit text, visibility and color for each player ---
        private string _p1HitText = string.Empty;
        private bool _p1HitVisible = false;
        private Brush _p1HitBrush = Brushes.White;

        private string _p2HitText = string.Empty;
        private bool _p2HitVisible = false;
        private Brush _p2HitBrush = Brushes.White;

        public string P1HitText
        {
            get => _p1HitText;
            set
            {
                if (_p1HitText != value)
                {
                    _p1HitText = value;
                    OnPropertyChanged(nameof(P1HitText));
                }
            }
        }

        public bool P1HitVisible
        {
            get => _p1HitVisible;
            set
            {
                if (_p1HitVisible != value)
                {
                    _p1HitVisible = value;
                    OnPropertyChanged(nameof(P1HitVisible));
                }
            }
        }

        public Brush P1HitBrush
        {
            get => _p1HitBrush;
            set
            {
                if (!Equals(_p1HitBrush, value))
                {
                    _p1HitBrush = value;
                    OnPropertyChanged(nameof(P1HitBrush));
                }
            }
        }

        public string P2HitText
        {
            get => _p2HitText;
            set
            {
                if (_p2HitText != value)
                {
                    _p2HitText = value;
                    OnPropertyChanged(nameof(P2HitText));
                }
            }
        }

        public bool P2HitVisible
        {
            get => _p2HitVisible;
            set
            {
                if (_p2HitVisible != value)
                {
                    _p2HitVisible = value;
                    OnPropertyChanged(nameof(P2HitVisible));
                }
            }
        }

        public Brush P2HitBrush
        {
            get => _p2HitBrush;
            set
            {
                if (!Equals(_p2HitBrush, value))
                {
                    _p2HitBrush = value;
                    OnPropertyChanged(nameof(P2HitBrush));
                }
            }
        }

        public Brush P1EllipseStroke
        {
            get => _p1EllipseStroke;
            set
            {
                if (!Equals(_p1EllipseStroke, value))
                {
                    _p1EllipseStroke = value;
                    OnPropertyChanged(nameof(P1EllipseStroke));
                }
            }
        }

        public Brush P1EllipseFill
        {
            get => _p1EllipseFill;
            set
            {
                if (!Equals(_p1EllipseFill, value))
                {
                    _p1EllipseFill = value;
                    OnPropertyChanged(nameof(P1EllipseFill));
                }
            }
        }

        public Brush P2EllipseStroke
        {
            get => _p2EllipseStroke;
            set
            {
                if (!Equals(_p2EllipseStroke, value))
                {
                    _p2EllipseStroke = value;
                    OnPropertyChanged(nameof(P2EllipseStroke));
                }
            }
        }

        public Brush P2EllipseFill
        {
            get => _p2EllipseFill;
            set
            {
                if (!Equals(_p2EllipseFill, value))
                {
                    _p2EllipseFill = value;
                    OnPropertyChanged(nameof(P2EllipseFill));
                }
            }
        }

        public InGameVM() : this(null) { }

        public InGameVM(Action onGameOver)
        {
            _onGameOver = onGameOver;

            // Initialize default brushes to match existing XAML colors
            _p1StrokeDefault = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x4E)); // #FF6B4E
            _p1FillDefault = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0x6B, 0x4E)); // #44FF6B4E

            _p2StrokeDefault = new SolidColorBrush(Color.FromRgb(0x4E, 0x9A, 0xFF)); // #4E9AFF
            _p2FillDefault = new SolidColorBrush(Color.FromArgb(0x44, 0x4E, 0x9A, 0xFF)); // #444E9AFF (semi-transparent blue)

            // Set initial public brushes
            P1EllipseStroke = _p1StrokeDefault;
            P1EllipseFill = _p1FillDefault;
            P2EllipseStroke = _p2StrokeDefault;
            P2EllipseFill = _p2FillDefault;

            AudioManager.StopBackgroundMusic();

            _musicManager = new MusicManager();
            _musicManager.MusicEnded += OnMusicEnded;

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

                if (GameModeManager.CurrentMode != null)
                {
                    GameModeManager.CurrentMode.Initialize();

                    var playersField = GameModeManager.CurrentMode.GetType().GetField("_players");
                    var players = playersField?.GetValue(GameModeManager.CurrentMode) as List<PlayerManager>;

                    if (players != null && players.Count > 0)
                    {
                        _engine = new GameEngine();
                        _engine.Initialize(players);
                        _engine.StartGame();

                        AudioManager.StopBackgroundMusic();
                        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Song", $"{SongManager.CurrentSong}.mp3");
                        _musicManager.Play(filePath);

                        // ✅ CHANGED: 开始渲染循环
                        StartRenderLoop();

                        OnPropertyChanged("");
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void StartRenderLoop()
        {
            if (_renderingAttached) return;
            CompositionTarget.Rendering += OnRendering;
            _renderingAttached = true;
        }

        private void StopRenderLoop()
        {
            if (!_renderingAttached) return;
            CompositionTarget.Rendering -= OnRendering;
            _renderingAttached = false;
        }

        // ✅ CHANGED: Rendering 回调（跟屏幕同步），并限帧到 60fps
        private void OnRendering(object sender, EventArgs e)
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            double t = _engine.CurrentTime; // ms
            if (t - _lastRenderUpdateMs < TargetFrameMs) return;
            _lastRenderUpdateMs = t;

            UpdateFrame();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            for (int i = 0; i < _engine.Players.Count; i++)
            {
                var settings = PlayerSettingsManager.GetSettings(i);
                if (settings.KeyBindings.ContainsValue(key))
                {
                    // Snapshot score counters before the key press so we can detect which counter changed.
                    var player = _engine.Players[i];
                    var score = player.Score;
                    var beforePerfect = score.PerfectHit;
                    var beforeGood = score.GoodHit;
                    var beforeBad = score.BadHit;
                    var beforeMiss = score.MissHit;

                    // Determine note color from binding key name (Red vs Blue)
                    var binding = settings.KeyBindings.FirstOrDefault(x => x.Value == key);
                    Note.NoteType noteType = binding.Key != null && binding.Key.Contains("Blue") ? Note.NoteType.Blue : Note.NoteType.Red;

                    // Perform the key handling (this updates score counters synchronously).
                    _engine.HandleKeyPress(i, key);

                    // Trigger a brief hit-circle color flash for the player who pressed
                    TriggerHitEffect(i, noteType);

                    // Detect which hit counter increased and show text accordingly.
                    string hitText = null;
                    Brush hitBrush = Brushes.White;
                    if (score.PerfectHit > beforePerfect)
                    {
                        hitText = "Perfect";
                        hitBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)); // Gold #FFD700
                    }
                    else if (score.GoodHit > beforeGood)
                    {
                        hitText = "Good";
                        hitBrush = new SolidColorBrush(Color.FromRgb(0x32, 0xCD, 0x32)); // LimeGreen
                    }
                    else if (score.BadHit > beforeBad)
                    {
                        hitText = "Bad";
                        hitBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x8C)); // Light red
                    }
                    else if (score.MissHit > beforeMiss)
                    {
                        hitText = "Miss";
                        hitBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00)); // Dark red
                    }

                    if (!string.IsNullOrEmpty(hitText))
                    {
                        ShowHitText(i, hitText, hitBrush);
                    }

                    e.Handled = true;
                    break;
                }
            }
        }

        private int _lastP1Score = int.MinValue;
        private int _lastP2Score = int.MinValue;

        private void UpdateFrame()
        {
            _engine.Update();

            _frameStamp++;
            UpdateNotes(0, Player1Notes, _p1Map, _frameStamp);

            if (IsMultiplayer)
                UpdateNotes(1, Player2Notes, _p2Map, _frameStamp);

            OnPropertyChanged(nameof(CurrentTime));

            var p1 = P1Score;
            if (p1 != _lastP1Score)
            {
                _lastP1Score = p1;
                OnPropertyChanged(nameof(P1Score));
            }

            if (IsMultiplayer)
            {
                var p2 = P2Score;
                if (p2 != _lastP2Score)
                {
                    _lastP2Score = p2;
                    OnPropertyChanged(nameof(P2Score));
                }
            }

            if (_engine.IsGameFinished())
            {
                StopRenderLoop();
                _engine.StopGame();
                AudioManager.StopBackgroundMusic();
                _onGameOver?.Invoke();
            }
        }

        private void UpdateNotes(
            int playerIdx,
            ObservableCollection<NoteVM> noteCollection,
            Dictionary<Note, NoteVM> map,
            int stamp)
        {
            if (_engine?.Players == null || playerIdx >= _engine.Players.Count) return;

            var player = _engine.Players[playerIdx];
            if (player?.noteManager == null) return;

            var activeNotes = player.noteManager.ActiveNotes;
            if (activeNotes == null) return;

            const double laneTop = 50;

            for (int i = 0; i < activeNotes.Count; i++)
            {
                var note = activeNotes[i];

                if (!map.TryGetValue(note, out var vm))
                {
                    vm = NoteVM.Create(note, laneTop);
                    map[note] = vm;
                    noteCollection.Add(vm);
                }

                vm.LastSeenStamp = stamp;
                vm.X = note.X - note.Speed * (1/60);
            }

            for (int i = noteCollection.Count - 1; i >= 0; i--)
            {
                var vm = noteCollection[i];
                if (vm.LastSeenStamp != stamp)
                {
                    map.Remove(vm.Model);
                    noteCollection.RemoveAt(i);
                }
            }
        }

        // Trigger a brief hit-circle flash for the given player index.
        // Uses async Task.Delay so no additional timers or removal of existing code required.
        private async void TriggerHitEffect(int playerIdx, Note.NoteType noteType)
        {
            const int flashMs = 120;

            // Play color-specific hit sound effect when a hit effect is triggered
            try
            {
                if (noteType == Note.NoteType.Red)
                    AudioManager.PlayFx("hit_red.wav");
                else
                    AudioManager.PlayFx("hit_blue.wav");
            }
            catch
            {
                // Swallow to avoid breaking gameplay on audio failure
            }

            if (playerIdx == 0)
            {
                // highlight (semi-white)
                P1EllipseStroke = new SolidColorBrush(Colors.White);
                P1EllipseFill = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF));
                await Task.Delay(flashMs);
                P1EllipseStroke = _p1StrokeDefault;
                P1EllipseFill = _p1FillDefault;
            }
            else if (playerIdx == 1)
            {
                P2EllipseStroke = new SolidColorBrush(Colors.White);
                P2EllipseFill = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF));
                await Task.Delay(flashMs);
                P2EllipseStroke = _p2StrokeDefault;
                P2EllipseFill = _p2FillDefault;
            }
        }

        // Show hit text above the player's ellipse briefly.
        private async void ShowHitText(int playerIdx, string text, Brush brush)
        {
            const int showMs = 600;

            if (playerIdx == 0)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    P1HitText = text;
                    P1HitBrush = brush;
                    P1HitVisible = true;
                });

                await Task.Delay(showMs);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    P1HitVisible = false;
                    P1HitText = string.Empty;
                });
            }
            else if (playerIdx == 1)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    P2HitText = text;
                    P2HitBrush = brush;
                    P2HitVisible = true;
                });

                await Task.Delay(showMs);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    P2HitVisible = false;
                    P2HitText = string.Empty;
                });
            }
        }

        public void Cleanup()
        {
            StopRenderLoop();

            var window = Application.Current.MainWindow;
            if (window != null)
                window.PreviewKeyDown -= OnKeyDown;

            if (_musicManager != null)
            {
                _musicManager.MusicEnded -= OnMusicEnded;
                _musicManager.Dispose();
            }

            _engine?.Dispose();

            _p1Map.Clear();
            _p2Map.Clear();
            Player1Notes.Clear();
            Player2Notes.Clear();
        }

        private void OnMusicEnded(object sender, EventArgs e)
        {
            _onGameOver?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NoteVM : INotifyPropertyChanged
    {
        private static readonly Brush RedFill = CreateFill(Color.FromRgb(255, 120, 120), Color.FromRgb(180, 0, 0));
        private static readonly Brush BlueFill = CreateFill(Color.FromRgb(120, 170, 255), Color.FromRgb(0, 100, 220));

        private static Brush CreateFill(Color inner, Color outer)
        {
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(inner, 0.3));
            brush.GradientStops.Add(new GradientStop(outer, 1.0));
            brush.Freeze();
            return brush;
        }

        public Note Model { get; }
        public int LastSeenStamp { get; set; }

        private double _x;
        private double _y;

        public double X
        {
            get => _x;
            set
            {
                if (Math.Abs(_x - value) > 0.01)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (Math.Abs(_y - value) > 0.01)
                {
                    _y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }

        public Brush Fill { get; }
        public Brush Border { get; }
        public string Icon { get; }

        private NoteVM(Note model, double laneTop)
        {
            Model = model;
            Y = laneTop;
            X = model.X - 30;

            if (model.Type == Note.NoteType.Red)
            {
                Fill = RedFill;
                Border = Brushes.DarkRed;
                Icon = "ド";
            }
            else
            {
                Fill = BlueFill;
                Border = Brushes.DarkBlue;
                Icon = "カ";
            }
        }

        public static NoteVM Create(Note model, double laneTop) => new NoteVM(model, laneTop);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
