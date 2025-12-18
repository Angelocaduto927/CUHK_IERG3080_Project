/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Diagnostics;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class GameEngine
    {
        private Stopwatch _stopwatch;
        private List<PlayerManager> _players;

        // ✅ 游戏状态（纯数据）
        public enum GameState { NotStarted, Playing, Finished }
        public GameState State { get; private set; }

        // ✅ 当前游戏时间（纯逻辑）
        public double CurrentTime => _stopwatch?.IsRunning == true ? _stopwatch.ElapsedMilliseconds : 0;

        // ✅ 只读的玩家列表（纯数据）
        public IReadOnlyList<PlayerManager> Players => _players?.AsReadOnly();

        public GameEngine()
        {
            _stopwatch = new Stopwatch();
            State = GameState.NotStarted;
        }

        // ✅ 初始化游戏（纯逻辑）
        public void Initialize(List<PlayerManager> players)
        {
            _players = players;
            State = GameState.NotStarted;
        }

        // ✅ 开始游戏（只管理时间和状态）
        public void StartGame()
        {
            if (State != GameState.NotStarted)
                return;

            _stopwatch.Start();
            State = GameState.Playing;
        }

        // ✅ 结束游戏
        public void StopGame()
        {
            _stopwatch.Stop();
            State = GameState.Finished;
        }

        // ✅ 重置游戏
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

        // ✅ Model 层的 Update（纯逻辑）
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

        // ✅ 处理按键（纯逻辑）
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

        // ✅ 检查游戏是否结束（纯逻辑）
        public bool IsGameFinished()
        {
            if (_players == null) return false;

            foreach (var player in _players)
            {
                if (player.Chart != null &&
                    player.Chart.Any(n => n.State == Note.NoteState.NotSpawned ||
                                          n.State == Note.NoteState.Active))
                {
                    return false;
                }
            }
            return true;
        }

        // ✅ 根据按键获取 NoteType（纯逻辑）
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

        // ✅ 清理资源
        public void Dispose()
        {
            _stopwatch?.Stop();
        }
    }
}
