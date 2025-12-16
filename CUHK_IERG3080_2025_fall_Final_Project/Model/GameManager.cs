using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class SpeedManager
    {
        public int CurrentSpeed { get; set; }
        public SpeedManager()
        {
            CurrentSpeed = 1;
        }
    }

    public static class  KeyManager
    {
       
    }

    public class ScoreManager
    {
        public int Score { get; private set; }
        public int PerfectHit { get; private set; }
        public int GoodHit { get; private set; }
        public int BadHit { get; private set; }
        public int MissHit { get; private set; }
        public int EarlyHit { get; private set; }
        public int LateHit { get; private set; }
        public int Combo {  get; private set; }
        public int MaxCombo { get; private set; }

        public ScoreManager()
        {
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

        public void Update(HitResult result, bool isEarly, bool isLate)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    PerfectHit++;
                    Score += ;
                    Combo++;
                    break;
                case HitResult.Good:
                    GoodHit++;
                    Score += ;
                    Combo++;
                    break;
                case HitResult.Bad:
                    BadHit++;
                    Score += ;
                    Combo++;
                    break;
                case HitResult.Miss:
                    MissHit++;
                    Combo = 0;
                    break;
            }
            if (isEarly) EarlyHit++;
            if (isLate) LateHit++;

            MaxCombo = Math.Max(Combo, MaxCombo);
        }

        public void Reset()
        {
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
        public ScoreManager Score {  get; private set; }
        public string PlayerName { get; set; }
        public bool IsLocalPlayer { get; set; }
        public string CurrentSong => SongManager.CurrentSong;
        public DifficultyManager Difficulty {  get; private set; }
        public SpeedManager Speed { get; private set; }
        public PlayerManager(int playIndex)
        {
            PlayerIndex = playIndex;
            Score =  new ScoreManager();
            PlayerName = $"Player {playIndex + 1}";
            IsLocalPlayer = true;
            Difficulty = new DifficultyManager();
            Speed = new SpeedManager();
        }
        public void Reset()
        {
            Score.Reset();
        }
        public void ProcessHit(HitResult result, bool isEarly = false, bool isLate = false)
        {
            Score.Update(result, isEarly, isLate);
        }
    }
}
