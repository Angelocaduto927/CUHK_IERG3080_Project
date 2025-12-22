using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    public class InGameVM : INotifyPropertyChanged, IDisposable
    {
        private OnlineSession _session;
        private bool _started;
        private bool _isOnline;
        private bool _startScheduled;
        private bool _disposed;
        private bool _leftOnline = false;

        private readonly TaskCompletionSource<bool> _engineReadyTcs =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private GameEngine _engine;
        private bool _initialized = false;
        private readonly Action _onGameOver;
        private readonly MusicManager _musicManager;

        private bool _renderingAttached = false;
        private double _lastRenderUpdateMs = -9999;
        private const double TargetFrameMs = 16.0;

        public ObservableCollection<NoteVM> Player1Notes { get; } = new ObservableCollection<NoteVM>();
        public ObservableCollection<NoteVM> Player2Notes { get; } = new ObservableCollection<NoteVM>();

        private readonly Dictionary<Note, NoteVM> _p1Map = new Dictionary<Note, NoteVM>();
        private readonly Dictionary<Note, NoteVM> _p2Map = new Dictionary<Note, NoteVM>();

        private int _frameStamp = 0;

        private readonly bool[] _netValid = new bool[3];
        private readonly int[] _netScore = new int[3];
        private readonly int[] _netCombo = new int[3];
        private readonly double[] _netAcc = new double[3];

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

        public bool IsMultiplayer => _isOnline || (_engine?.Players?.Count > 1);

        public string P1Name => (_engine?.Players != null && _engine.Players.Count > 0) ? _engine.Players[0].PlayerName : "Player 1";
        public int P1Score => GetDisplayedScore(slot: 1);
        public int P1Combo => GetDisplayedCombo(slot: 1);
        public double P1Accuracy => GetDisplayedAccuracy(slot: 1);

        public string P2Name => (_engine?.Players != null && _engine.Players.Count > 1) ? _engine.Players[1].PlayerName : "Player 2";
        public int P2Score => GetDisplayedScore(slot: 2);
        public int P2Combo => GetDisplayedCombo(slot: 2);
        public double P2Accuracy => GetDisplayedAccuracy(slot: 2);

        private bool IsLocalSlot(int slot)
        {
            return _isOnline && _session != null && _session.IsConnected && _session.LocalSlot == slot;
        }

        private int GetDisplayedScore(int slot)
        {
            int idx = slot - 1;
            int engineScore = (_engine?.Players != null && idx >= 0 && idx < _engine.Players.Count) ? _engine.Players[idx].Score.Score : 0;

            if (!_isOnline) return engineScore;
            if (IsLocalSlot(slot)) return engineScore;
            if (_netValid[slot]) return _netScore[slot];
            return engineScore;
        }

        private int GetDisplayedCombo(int slot)
        {
            int idx = slot - 1;
            int engineCombo = (_engine?.Players != null && idx >= 0 && idx < _engine.Players.Count) ? _engine.Players[idx].Score.Combo : 0;

            if (!_isOnline) return engineCombo;
            if (IsLocalSlot(slot)) return engineCombo;
            if (_netValid[slot]) return _netCombo[slot];
            return engineCombo;
        }

        private double GetDisplayedAccuracy(int slot)
        {
            int idx = slot - 1;
            double engineAcc = (_engine?.Players != null && idx >= 0 && idx < _engine.Players.Count) ? _engine.Players[idx].Score.Accuracy : 100;

            if (!_isOnline) return engineAcc;
            if (IsLocalSlot(slot)) return engineAcc;
            if (_netValid[slot]) return _netAcc[slot];
            return engineAcc;
        }

        private readonly Brush _p1StrokeDefault;
        private readonly Brush _p1FillDefault;
        private readonly Brush _p2StrokeDefault;
        private readonly Brush _p2FillDefault;

        private Brush _p1EllipseStroke;
        private Brush _p1EllipseFill;
        private Brush _p2EllipseStroke;
        private Brush _p2EllipseFill;

        private string _p1HitText = string.Empty;
        private bool _p1HitVisible = false;
        private Brush _p1HitBrush = Brushes.White;

        private string _p2HitText = string.Empty;
        private bool _p2HitVisible = false;
        private Brush _p2HitBrush = Brushes.White;

        public string P1HitText { get => _p1HitText; set { if (_p1HitText != value) { _p1HitText = value; OnPropertyChanged(nameof(P1HitText)); } } }
        public bool P1HitVisible { get => _p1HitVisible; set { if (_p1HitVisible != value) { _p1HitVisible = value; OnPropertyChanged(nameof(P1HitVisible)); } } }
        public Brush P1HitBrush { get => _p1HitBrush; set { if (!Equals(_p1HitBrush, value)) { _p1HitBrush = value; OnPropertyChanged(nameof(P1HitBrush)); } } }

        public string P2HitText { get => _p2HitText; set { if (_p2HitText != value) { _p2HitText = value; OnPropertyChanged(nameof(P2HitText)); } } }
        public bool P2HitVisible { get => _p2HitVisible; set { if (_p2HitVisible != value) { _p2HitVisible = value; OnPropertyChanged(nameof(P2HitVisible)); } } }
        public Brush P2HitBrush { get => _p2HitBrush; set { if (!Equals(_p2HitBrush, value)) { _p2HitBrush = value; OnPropertyChanged(nameof(P2HitBrush)); } } }

        public Brush P1EllipseStroke { get => _p1EllipseStroke; set { if (!Equals(_p1EllipseStroke, value)) { _p1EllipseStroke = value; OnPropertyChanged(nameof(P1EllipseStroke)); } } }
        public Brush P1EllipseFill { get => _p1EllipseFill; set { if (!Equals(_p1EllipseFill, value)) { _p1EllipseFill = value; OnPropertyChanged(nameof(P1EllipseFill)); } } }
        public Brush P2EllipseStroke { get => _p2EllipseStroke; set { if (!Equals(_p2EllipseStroke, value)) { _p2EllipseStroke = value; OnPropertyChanged(nameof(P2EllipseStroke)); } } }
        public Brush P2EllipseFill { get => _p2EllipseFill; set { if (!Equals(_p2EllipseFill, value)) { _p2EllipseFill = value; OnPropertyChanged(nameof(P2EllipseFill)); } } }

        public InGameVM() : this(null) { }

        public InGameVM(Action onGameOver)
        {
            _onGameOver = onGameOver;

            _p1StrokeDefault = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x4E));
            _p1FillDefault = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0x6B, 0x4E));
            _p2StrokeDefault = new SolidColorBrush(Color.FromRgb(0x4E, 0x9A, 0xFF));
            _p2FillDefault = new SolidColorBrush(Color.FromArgb(0x44, 0x4E, 0x9A, 0xFF));

            P1EllipseStroke = _p1StrokeDefault;
            P1EllipseFill = _p1FillDefault;
            P2EllipseStroke = _p2StrokeDefault;
            P2EllipseFill = _p2FillDefault;

            AudioManager.StopBackgroundMusic();

            _musicManager = new MusicManager();
            _musicManager.MusicEnded += OnMusicEnded;

            _isOnline = GameModeManager.CurrentMode is OnlineMultiPlayerMode
                        && GameModeManager.OnlineSession != null
                        && GameModeManager.OnlineSession.IsConnected;

            if (_isOnline)
            {
                _session = GameModeManager.OnlineSession;

                _localPlayerIndex = Math.Max(0, (_session.LocalSlot - 1));

                _session.OnStart += OnStartFromNet;

                _session.OnInput += OnInputFromNet;

                _session.OnHitResult += OnHitResultFromNet;

                if (_session.LastStartMsg != null)
                    OnStartFromNet(_session.LastStartMsg);
            }

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
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

                    if (_isOnline && players != null)
                    {
                        while (players.Count < 2)
                            players.Add(new PlayerManager(players.Count, isLocalPlayer: false));
                    }

                    if (players != null && players.Count > 0)
                    {
                        _engine = new GameEngine();
                        _engine.Initialize(players);

                        AudioManager.StopBackgroundMusic();

                        _engineReadyTcs.TrySetResult(true);

                        if (!_isOnline)
                            StartLocalGameNow();

                        OnPropertyChanged(nameof(IsMultiplayer));
                        OnPropertyChanged("");
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnStartFromNet(StartMsg msg)
        {
            if (msg == null) return;
            if (_disposed) return;
            if (_startScheduled) return;
            _startScheduled = true;

            _ = BeginStartAtAsync(msg);
        }

        private async Task BeginStartAtAsync(StartMsg msg)
        {
            await _engineReadyTcs.Task;

            if (_disposed) return;
            if (_started) return;
            _started = true;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long startAt = (msg.StartAtUnixMs > 0) ? msg.StartAtUnixMs : now + msg.StartInMs;

            int delayMs = (int)Math.Max(0, startAt - now);
            if (delayMs > 0) await Task.Delay(delayMs);

            if (_disposed) return;
            Application.Current.Dispatcher.Invoke(StartLocalGameNow);
        }

        private void StartLocalGameNow()
        {
            if (_engine == null) return;
            if (_engine.State == GameEngine.GameState.Playing) return;

            AudioManager.StopBackgroundMusic();

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "Song", $"{SongManager.CurrentSong}.mp3");

            _musicManager.Play(filePath);

            _engine.StartGame();
            StartRenderLoop();

            OnPropertyChanged("");
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

        private void OnRendering(object sender, EventArgs e)
        {
            if (_disposed) return;
            if (_engine?.State != GameEngine.GameState.Playing) return;

            double t = _engine.CurrentTime;
            if (t - _lastRenderUpdateMs < TargetFrameMs) return;
            _lastRenderUpdateMs = t;

            UpdateFrame();
        }

        private int _localPlayerIndex = 0;

        private void PlayTapSound(Note.NoteType noteType)
        {
            try
            {
                if (noteType == Note.NoteType.Red) AudioManager.PlayFx("hit_red.wav");
                else AudioManager.PlayFx("hit_blue.wav");
            }
            catch { }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_disposed) return;
            if (_engine?.State != GameEngine.GameState.Playing) return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            int start = 0;
            int end = _engine.Players.Count;

            if (_isOnline)
            {
                start = Math.Max(0, Math.Min(_localPlayerIndex, _engine.Players.Count - 1));
                end = start + 1;
            }

            for (int i = start; i < end; i++)
            {
                var settings = PlayerSettingsManager.GetSettings(i);
                if (!settings.KeyBindings.ContainsValue(key))
                    continue;

                var binding = settings.KeyBindings.FirstOrDefault(x => x.Value == key);
                Note.NoteType noteType =
                    (binding.Key != null && binding.Key.Contains("Blue"))
                        ? Note.NoteType.Blue
                        : Note.NoteType.Red;

                PlayTapSound(noteType);

                TriggerHitEffect(i, noteType);

                if (_isOnline && _session != null && _session.IsConnected)
                {
                    try
                    {
                        _ = _session.SendInputAsync(_session.LocalSlot, noteType.ToString(), _engine.CurrentTime);
                    }
                    catch { }
                }

                var player = _engine.Players[i];
                var score = player.Score;
                var beforePerfect = score.PerfectHit;
                var beforeGood = score.GoodHit;
                var beforeBad = score.BadHit;
                var beforeMiss = score.MissHit;

                _engine.HandleKeyPress(i, key);

                bool isPerfect = score.PerfectHit > beforePerfect;
                bool isGood = score.GoodHit > beforeGood;
                bool isBad = score.BadHit > beforeBad;
                bool isMiss = score.MissHit > beforeMiss;

                string result = "Tap";
                string hitText = null;
                Brush hitBrush = Brushes.White;

                if (isPerfect)
                {
                    result = "Perfect";
                    hitText = "Perfect";
                    hitBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
                }
                else if (isGood)
                {
                    result = "Good";
                    hitText = "Good";
                    hitBrush = new SolidColorBrush(Color.FromRgb(0x32, 0xCD, 0x32));
                }
                else if (isBad)
                {
                    result = "Bad";
                    hitText = "Bad";
                    hitBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x8C));
                }
                else if (isMiss)
                {
                    result = "Miss";
                    hitText = "Miss";
                    hitBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00));
                }

                if (!string.IsNullOrEmpty(hitText))
                    ShowHitText(i, hitText, hitBrush);

                if (_isOnline && _session != null && _session.IsConnected)
                {
                    try
                    {
                        _ = _session.SendHitResultAsync(
                            slot: _session.LocalSlot,
                            noteType: noteType.ToString(),
                            atMs: _engine.CurrentTime,
                            result: result,
                            score: score.Score,
                            combo: score.Combo,
                            accuracy: score.Accuracy
                        );
                    }
                    catch { }
                }

                e.Handled = true;
                return;
            }
        }

        private void OnInputFromNet(InputMsg msg)
        {
            if (_disposed) return;
            if (!_isOnline) return;
            if (msg == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_disposed) return;
                _ = ApplyRemoteInputAtAsync(msg);
            }));
        }

        private async Task ApplyRemoteInputAtAsync(InputMsg msg)
        {
            await _engineReadyTcs.Task;
            if (_disposed) return;

            int waitGuard = 0;
            while (!_disposed && (_engine == null || _engine.State != GameEngine.GameState.Playing))
            {
                await Task.Delay(10);
                if (++waitGuard > 2000) return;
            }
            if (_disposed) return;

            int idx = msg.Slot - 1;
            if (idx < 0 || _engine?.Players == null || idx >= _engine.Players.Count) return;

            if (_session != null && msg.Slot == _session.LocalSlot) return;

            double targetMs = msg.AtMs;
            double nowMs = _engine.CurrentTime;
            int delay = (int)Math.Max(0, targetMs - nowMs);
            if (delay > 0) await Task.Delay(delay);

            if (_disposed) return;
            if (_engine?.State != GameEngine.GameState.Playing) return;

            if (!TryMapNoteTypeToKey(idx, msg.NoteType, out var key))
                return;

            _engine.HandleKeyPress(idx, key);
        }

        private void OnHitResultFromNet(HitResultMsg msg)
        {
            if (_disposed) return;
            if (!_isOnline) return;
            if (msg == null) return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_disposed) return;
                _ = ApplyRemoteHitResultAtAsync(msg);
            }));
        }

        private async Task ApplyRemoteHitResultAtAsync(HitResultMsg msg)
        {
            await _engineReadyTcs.Task;
            if (_disposed) return;

            int waitGuard = 0;
            while (!_disposed && (_engine == null || _engine.State != GameEngine.GameState.Playing))
            {
                await Task.Delay(10);
                if (++waitGuard > 2000) return;
            }
            if (_disposed) return;

            if (_session != null && msg.Slot == _session.LocalSlot) return;

            double targetMs = msg.AtMs;
            double nowMs = _engine.CurrentTime;
            int delay = (int)Math.Max(0, targetMs - nowMs);
            if (delay > 0) await Task.Delay(delay);

            if (_disposed) return;
            if (_engine?.State != GameEngine.GameState.Playing) return;

            int slot = msg.Slot;
            if (slot != 1 && slot != 2) return;

            _netValid[slot] = true;
            _netScore[slot] = msg.Score;
            _netCombo[slot] = msg.Combo;
            _netAcc[slot] = msg.Accuracy;

            if (slot == 1)
            {
                OnPropertyChanged(nameof(P1Score));
                OnPropertyChanged(nameof(P1Combo));
                OnPropertyChanged(nameof(P1Accuracy));
            }
            else
            {
                OnPropertyChanged(nameof(P2Score));
                OnPropertyChanged(nameof(P2Combo));
                OnPropertyChanged(nameof(P2Accuracy));
            }

            int playerIdx = slot - 1;
            var noteTypeEnum = ParseNoteType(msg.NoteType);

            TriggerHitEffect(playerIdx, noteTypeEnum);

            var result = msg.Result ?? "Tap";
            if (!result.Equals("Tap", StringComparison.OrdinalIgnoreCase))
            {
                var brush = BrushForResult(result);
                ShowHitText(playerIdx, result, brush);
            }
        }

        private static Brush BrushForResult(string result)
        {
            if (string.IsNullOrWhiteSpace(result)) return Brushes.White;

            if (result.Equals("Perfect", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
            if (result.Equals("Good", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(0x32, 0xCD, 0x32));
            if (result.Equals("Bad", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x8C));
            if (result.Equals("Miss", StringComparison.OrdinalIgnoreCase))
                return new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00));

            return Brushes.White;
        }

        private static Note.NoteType ParseNoteType(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Note.NoteType.Red;
            return s.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0
                ? Note.NoteType.Blue
                : Note.NoteType.Red;
        }

        private bool TryMapNoteTypeToKey(int playerIdx, string noteType, out Key key)
        {
            key = Key.None;

            var settings = PlayerSettingsManager.GetSettings(playerIdx);
            if (settings == null || settings.KeyBindings == null) return false;

            bool wantBlue = noteType != null && noteType.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0;
            string wantWord = wantBlue ? "Blue" : "Red";

            foreach (var kv in settings.KeyBindings)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Key.IndexOf(wantWord, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    key = kv.Value;
                    return true;
                }
            }

            var any = settings.KeyBindings.FirstOrDefault();
            if (!Equals(any.Value, Key.None))
            {
                key = any.Value;
                return true;
            }

            return false;
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
                LeaveOnlineOnce("Game finished", sendSummary: true);

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
            const double xOffset = 30;

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
                vm.X = note.X - xOffset;
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

        private async void TriggerHitEffect(int playerIdx, Note.NoteType noteType)
        {
            const int flashMs = 120;

            Brush flashFill =
                (noteType == Note.NoteType.Red)
                    ? new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0x80, 0x80))
                    : new SolidColorBrush(Color.FromArgb(0x88, 0x80, 0xA8, 0xFF));

            if (playerIdx == 0)
            {
                P1EllipseStroke = new SolidColorBrush(Colors.White);
                P1EllipseFill = flashFill;
                await Task.Delay(flashMs);
                if (_disposed) return;
                P1EllipseStroke = _p1StrokeDefault;
                P1EllipseFill = _p1FillDefault;
            }
            else if (playerIdx == 1)
            {
                P2EllipseStroke = new SolidColorBrush(Colors.White);
                P2EllipseFill = flashFill;
                await Task.Delay(flashMs);
                if (_disposed) return;
                P2EllipseStroke = _p2StrokeDefault;
                P2EllipseFill = _p2FillDefault;
            }
        }

        private async void ShowHitText(int playerIdx, string text, Brush brush)
        {
            const int showMs = 600;

            if (playerIdx == 0)
            {
                P1HitText = text;
                P1HitBrush = brush;
                P1HitVisible = true;

                await Task.Delay(showMs);
                if (_disposed) return;

                P1HitVisible = false;
                P1HitText = string.Empty;
            }
            else if (playerIdx == 1)
            {
                P2HitText = text;
                P2HitBrush = brush;
                P2HitVisible = true;

                await Task.Delay(showMs);
                if (_disposed) return;

                P2HitVisible = false;
                P2HitText = string.Empty;
            }
        }

        private MatchSummaryMsg BuildLocalSummary()
        {
            try
            {
                if (_engine?.Players == null || _engine.Players.Count == 0) return null;

                int idx = 0;
                if (_isOnline)
                    idx = Math.Max(0, Math.Min(_localPlayerIndex, _engine.Players.Count - 1));

                var p = _engine.Players[idx];
                var sc = p?.Score;
                if (sc == null) return null;

                int slot = (_isOnline && _session != null && _session.LocalSlot > 0) ? _session.LocalSlot : (idx + 1);

                int totalNotes = (int)sc.PerfectHit + (int)sc.GoodHit + (int)sc.BadHit + (int)sc.MissHit;

                return new MatchSummaryMsg
                {
                    Slot = slot,
                    PlayerName = p?.PlayerName ?? ("Player " + slot),

                    Score = (int)sc.Score,
                    PerfectHit = (int)sc.PerfectHit,
                    GoodHit = (int)sc.GoodHit,
                    BadHit = (int)sc.BadHit,
                    MissHit = (int)sc.MissHit,

                    MaxCombo = (int)sc.MaxCombo,
                    TotalNotes = totalNotes,
                    Accuracy = (double)sc.Accuracy
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task TrySendMatchSummaryAsync(MatchSummaryMsg summary)
        {
            if (summary == null) return;
            if (_session == null || !_session.IsConnected) return;

            try
            {
                MethodInfo mi = _session.GetType().GetMethod("SendMatchSummaryAsync");
                if (mi == null) return;

                var t = mi.Invoke(_session, new object[] { summary }) as Task;
                if (t != null) await t.ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private void LeaveOnlineOnce(string reason, bool sendSummary)
        {
            if (_leftOnline) return;
            if (!_isOnline) return;
            if (_session == null) return;

            _leftOnline = true;

            if (!sendSummary)
            {
                try { _ = _session.LeaveAsync(reason); } catch { }
                return;
            }

            var summary = BuildLocalSummary();

            _ = Task.Run(async () =>
            {
                try { await TrySendMatchSummaryAsync(summary).ConfigureAwait(false); } catch { }
                try { await Task.Delay(200).ConfigureAwait(false); } catch { }

                try { await _session.LeaveAsync(reason).ConfigureAwait(false); } catch { }
            });
        }

        public void Cleanup()
        {
            if (_disposed) return;
            _disposed = true;

            LeaveOnlineOnce("Cleanup", sendSummary: false);

            StopRenderLoop();

            if (_session != null)
            {
                _session.OnStart -= OnStartFromNet;
                _session.OnInput -= OnInputFromNet;
                _session.OnHitResult -= OnHitResultFromNet;
            }

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

        public void Dispose()
        {
            Cleanup();
        }

        private void OnMusicEnded(object sender, EventArgs e)
        {
            LeaveOnlineOnce("Music ended", sendSummary: true);
            StopRenderLoop();

            if (_engine != null && _engine.State == GameEngine.GameState.Playing)
            {
                _engine.StopGame();
            }

            AudioManager.StopBackgroundMusic();
            _disposed = true;

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
