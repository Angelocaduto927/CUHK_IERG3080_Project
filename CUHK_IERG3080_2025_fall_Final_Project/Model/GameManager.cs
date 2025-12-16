using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string CurrentDifficulty { get; private set; }
        public void SetDifficulty(string difficulty) {
            CurrentDifficulty = difficulty;
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
        
    }

    public class PlayerManager
    {
        string SelectedSong { get; set; }       

    }
}
