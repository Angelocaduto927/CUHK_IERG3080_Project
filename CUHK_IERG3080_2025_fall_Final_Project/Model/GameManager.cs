using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public static class GameManager
    {
    }

    public static class GameModeManager
    {
        //the current game mode
        public static IGameMode CurrentMode { get; private set; }
        public enum Mode
        {
            SinglePlayer,
            LocalMultiPlayer,
            OnlineMultiPlayer
        }
        public static void SetMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.SinglePlayer:
                    CurrentMode = new SinglePlayerMode();
                    break;
                case Mode.LocalMultiPlayer:
                    CurrentMode = new LocalMultiPlayerMode();
                    break;
                case Mode.OnlineMultiPlayer:
                    CurrentMode = new OnlineMultiPlayerMode();
                    break;
            }
        }
    }

    public static class SongManager
    {
        public static string CurrentSong { get; private set; }
        public static void SetSong(string song)
        {
            CurrentSong = song;
        }
    }

    public class DifficultyManager
    {
        public string CurrentDifficulty { get; set; }
        public DifficultyManager()
        {
            CurrentDifficulty = "Easy";
        }
    }

    /*
    public class SpeedManager
    {
        public int CurrentSpeed { get; set; }
        public SpeedManager()
        {
            CurrentSpeed = 1;
        }
    }
    */

    public static class  KeyManager
    {
       
    }

    public class ScoreManager
    {
        private ScoreSet _scoreset;
        public int Score { get; private set; }
        public int PerfectHit { get; private set; }
        public int GoodHit { get; private set; }
        public int BadHit { get; private set; }
        public int MissHit { get; private set; }
        public int EarlyHit { get; private set; }
        public int LateHit { get; private set; }
        public int Combo {  get; private set; }
        public int MaxCombo { get; private set; }

        public ScoreManager(ScoreSet scoreset)
        {
            _scoreset = scoreset;
            Score = 0;
            PerfectHit = 0;
            GoodHit = 0;
            BadHit = 0;
            MissHit = 0;
            EarlyHit = 0;
            LateHit = 0;
            Combo = 0;
            MaxCombo = 0;
        }
        public void SetScoreSet(ScoreSet scoreset)
        {
            _scoreset = scoreset;
        }
        public void Update(HitResult result, bool isEarly, bool isLate)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    PerfectHit++;
                    Score += _scoreset.PerfectHitScore;
                    Combo++;
                    break;
                case HitResult.Good:
                    GoodHit++;
                    Score += _scoreset.GoodHitScore;
                    Combo++;
                    break;
                case HitResult.Bad:
                    BadHit++;
                    Score += _scoreset.BadHitScore;
                    Combo++;
                    break;
                case HitResult.Miss:
                    MissHit++;
                    Score += _scoreset.MissHitScore;
                    Combo = 0;
                    break;
            }
            if (isEarly) EarlyHit++;
            if (isLate) LateHit++;

            MaxCombo = Math.Max(Combo, MaxCombo);
        }

        public void Reset(ScoreSet scoreset)
        {
            _scoreset = scoreset;
            Score = 0;
            GoodHit = 0;
            PerfectHit = 0;
            BadHit = 0;
            MissHit = 0;
            EarlyHit = 0;
            LateHit = 0;
            Combo = 0;
            MaxCombo = 0;
        }
    }

    public enum HitResult
    {
        Perfect,
        Good,
        Bad,
        Miss
    }

    public class PlayerManager
    {
        public int PlayerIndex { get; private set; }
        public string PlayerName { get; set; }
        public bool IsLocalPlayer { get; set; }
        public double Speed { get; set; }
        public string CurrentSong => SongManager.CurrentSong;
        public ScoreManager Score { get; private set; }
        public DifficultyManager Difficulty {  get; private set; }
        public ScoreSet ScoreSet { get; set; }
        public List<Note> Chart { get; set; }
        public PlayerManager(int playerIndex, bool isLocalPlayer = true)
        {
            PlayerIndex = playerIndex;
            IsLocalPlayer = isLocalPlayer;
            PlayerName = $"Player {playerIndex + 1}";
            ScoreSet = new ScoreSet();
            Score =  new ScoreManager(ScoreSet);
            Difficulty = new DifficultyManager();
            Speed = Hyperparameters.DefaultSpeed;
            Chart = new List<Note>();
        }
        public void ProcessHit(HitResult result, bool isEarly = false, bool isLate = false)
        {
            Score.Update(result, isEarly, isLate);
        }
    }
}
