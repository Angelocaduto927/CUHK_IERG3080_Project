using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class OnlineMultiPlayerMode : IGameMode
    {
        public int PlayerCount { get; }
        public List<PlayerManager> _players;
        public string ModeName => "Online Multi Player";
        public void Initialize()
        {
            foreach (PlayerManager player in _players)
            {
                string chartPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chart", $"{SongManager.CurrentSong}_{player.Difficulty.CurrentDifficulty}.json");
                (player.Chart, player.ScoreSet) = new JsonLoader().LoadFromJson(chartPath);
                player.Score.SetScoreSet(player.ScoreSet);
                player.noteManager.Set_allNotes(player.Chart);
                foreach (Note note in player.Chart)
                {
                    note.X = Hyperparameters.SpawnZoneXCoordinate;
                    note.Y = Hyperparameters.SinglePlayerYCoordinate;
                    note.Speed = player.Speed;
                    note.SpawnTime = note.HitTime - (Hyperparameters.Length / note.Speed);
                }
            }
        }
        public OnlineMultiPlayerMode()
        {
            PlayerCount = 2; // Default to 2 players for online multiplayer
            _players = new List<PlayerManager>();
            CreatePlayers();
        }
        public void CreatePlayers()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerManager player = new PlayerManager(playerIndex: i, isLocalPlayer: false);
                _players.Add(player);
            }
        }
    }
}
