using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class GameEngine
    {
        private readonly Stopwatch _stopwatch;
        private List<PlayerManager> _players;
        private double? _songDurationMs;

        public enum GameState { NotStarted, Playing, Finished }
        public GameState State { get; private set; }

        public double CurrentTime => _stopwatch.IsRunning ? _stopwatch.ElapsedMilliseconds : 0;

        public IReadOnlyList<PlayerManager> Players => _players?.AsReadOnly();

        public GameEngine()
        {
            _stopwatch = new Stopwatch();
            State = GameState.NotStarted;
        }

        public void SetSongDuration(double durationMs)
        {
            if (durationMs <= 0)
            {
                _songDurationMs = null;
                return;
            }
            _songDurationMs = durationMs;
        }

        public void UpdatePlayerSpeed(int playerIndex, int newSpeed)
        {
            if (_players != null && playerIndex >= 0 && playerIndex < _players.Count)
            {
                _players[playerIndex].UpdateSpeed(newSpeed);
            }
        }

        public void Initialize(List<PlayerManager> players)
        {
            _players = players;
            State = GameState.NotStarted;
        }

        public void StartGame()
        {
            if (State != GameState.NotStarted)
                return;

            _stopwatch.Restart(); // ✅ CHANGED: 用 Restart 确保从 0 开始且运行
            State = GameState.Playing;
        }

        public void StopGame()
        {
            _stopwatch.Stop();
            State = GameState.Finished;
        }

        public void ResetGame()
        {
            _stopwatch.Reset();

            if (_players != null)
            {
                foreach (var player in _players)
                {
                    player.noteManager?.Reset();
                    player.Score.Reset(player.ScoreSet);
                }
            }

            State = GameState.NotStarted;
        }

        public void Update()
        {
            if (State != GameState.Playing)
                return;

            double currentTime = CurrentTime;

            if (_players != null)
            {
                foreach (var player in _players)
                {
                    player.noteManager?.Update(currentTime);
                }
            }
        }

        public void HandleKeyPress(int playerIndex, System.Windows.Input.Key key)
        {
            if (State != GameState.Playing)
                return;

            if (_players == null || playerIndex < 0 || playerIndex >= _players.Count)
                return;

            var player = _players[playerIndex];
            var noteType = GetNoteTypeFromKey(key, playerIndex);

            if (noteType.HasValue)
            {
                player.noteManager?.HitNote(CurrentTime, noteType.Value);
            }
        }

        // ✅ CHANGED: 不再每帧扫整张 Chart.Any(...)
        // 结束条件应该是：没有待生成的 note 且没有 active note
        // NoteManager.HasPendingNotes 是 O(1)
        public bool IsGameFinished()
        {
            if (State != GameState.Playing)
                return false;
            if (_songDurationMs.HasValue)
            {
                return CurrentTime >= _songDurationMs.Value;
            }
            if (_players == null || _players.Count == 0)
                return false;

            foreach (var player in _players)
            {
                if (player?.noteManager != null && player.noteManager.HasPendingNotes)
                    return false;
            }

            return false;
        }

        private Note.NoteType? GetNoteTypeFromKey(System.Windows.Input.Key key, int playerIndex)
        {
            var settings = PlayerSettingsManager.GetSettings(playerIndex);

            if (settings.KeyBindings.ContainsValue(key))
            {
                var binding = settings.KeyBindings.FirstOrDefault(x => x.Value == key);
                if (binding.Key.Contains("Red"))
                    return Note.NoteType.Red;
                if (binding.Key.Contains("Blue"))
                    return Note.NoteType.Blue;
            }

            return null;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
        }
    }
}
