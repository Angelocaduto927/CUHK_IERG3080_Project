using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

        public InGameVM() : this(null) { }

        public InGameVM(Action onGameOver)
        {
            _onGameOver = onGameOver;

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
                    _engine.HandleKeyPress(i, key);
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
                vm.X = note.X - 30;
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
