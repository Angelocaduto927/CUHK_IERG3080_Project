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

    public class PlayerSettings
    {
        public double Speed { get; set; }
        public Dictionary<string, System.Windows.Input.Key> KeyBindings { get; set; }
        public PlayerSettings()
        {
            Speed = Hyperparameters.DefaultSpeed;
            KeyBindings = new Dictionary<string, System.Windows.Input.Key>();
        }
    }

    public static class PlayerSettingsManager
    {
        private static Dictionary<int, PlayerSettings> _playerSettings = new Dictionary<int, PlayerSettings>();
        static PlayerSettingsManager()
        {
            _playerSettings[0] = new PlayerSettings
            {
                KeyBindings = new Dictionary<string, System.Windows.Input.Key>
                {
                    { "Red1", System.Windows.Input.Key.D },
                    { "Red2", System.Windows.Input.Key.F },
                    { "Blue1", System.Windows.Input.Key.J },
                    { "Blue2", System.Windows.Input.Key.K }
                }
            };
            _playerSettings[1] = new PlayerSettings
            {
                Speed = 1.0,
                KeyBindings = new Dictionary<string, System.Windows.Input.Key>
                {
                    { "Red1", System.Windows.Input.Key.Q },
                    { "Red2", System.Windows.Input.Key.W },
                    { "Blue1", System.Windows.Input.Key.O },
                    { "Blue2", System.Windows.Input.Key.P }
                }
            };
        }
        public static PlayerSettings GetSettings(int playerIndex)
        {
            return _playerSettings.ContainsKey(playerIndex)
                ? _playerSettings[playerIndex]
                : new PlayerSettings();
        }

        public static void UpdateSettings(int playerIndex, PlayerSettings settings)
        {
            _playerSettings[playerIndex] = settings;
        }

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
        public double Accuracy { get; private set; }
        public int TotalPassedNotes { get; private set; }


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
            Accuracy = 100;
            TotalPassedNotes = 0;
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
            TotalPassedNotes++;
            Accuracy = (PerfectHit + GoodHit * Hyperparameters.GoodWeight + BadHit * Hyperparameters.BadWeight) * 100 / TotalPassedNotes;
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
        public Dictionary<string, System.Windows.Input.Key> KeysDict { get; set; }
        public string CurrentSong => SongManager.CurrentSong;
        public ScoreManager Score { get; private set; }
        public DifficultyManager Difficulty {  get; private set; }
        public ScoreSet ScoreSet { get; set; }
        public List<Note> Chart { get; set; }
        public NoteManager noteManager { get; set; }
        public PlayerManager(int playerIndex, bool isLocalPlayer = true)
        {
            PlayerIndex = playerIndex;
            IsLocalPlayer = isLocalPlayer;
            PlayerName = $"Player {playerIndex + 1}";
            ScoreSet = new ScoreSet();
            Score =  new ScoreManager(ScoreSet);
            Difficulty = new DifficultyManager();
            //Speed = Hyperparameters.DefaultSpeed;
            Speed = PlayerSettingsManager.GetSettings(playerIndex).Speed;
            KeysDict = PlayerSettingsManager.GetSettings(playerIndex).KeyBindings;
            Chart = new List<Note>();
            noteManager = new NoteManager(Chart, this);
        }
    }
}
