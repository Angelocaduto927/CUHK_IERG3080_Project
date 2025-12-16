using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class LocalMultiPlayerMode : IGameMode
    {
        public int PlayerCount { get; }
        public List<PlayerManager> _players;
        private GameEngine _engine;
        public string ModeName => "Local Multi Player";
        public void Initialize()
        {
            _engine = new GameEngine();
            foreach (PlayerManager player in _players)
            {
                (player.Chart, player.ScoreSet) = new JsonLoader().LoadFromJson($"{SongManager.CurrentSong}_{player.Difficulty}.json");
                player.Score.SetScoreSet(player.ScoreSet);
                for (int i = 0; i < player.Chart.Count; i++)
                {
                    Note note = player.Chart[i];
                    note.X = Hyperparameters.SpawnZoneXCoordinate;
                    if (player.PlayerIndex == 1)
                    {
                        note.Y = Hyperparameters.MultiPlayerUpperYCoordinate;
                    }
                    else
                    {
                        note.Y = Hyperparameters.MultiPlayerLowerYCoordinate;
                    }
                    note.Speed = player.Speed;
                    note.SpawnTime = note.HitTime - (Hyperparameters.Length / note.Speed);
                }
            }
            _engine.StartGame(_players);
        }
        public LocalMultiPlayerMode()
        {
            PlayerCount = 2; // Default to 2 players for local multiplayer
            _players = new List<PlayerManager>();
            CreatePlayers();
        }
        public void CreatePlayers()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerManager player = new PlayerManager(playerIndex: i + 1, isLocalPlayer: true);
                _players.Add(player);
            }
        }
    }
}
