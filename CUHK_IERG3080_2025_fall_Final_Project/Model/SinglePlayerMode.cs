using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class SinglePlayerMode : IGameMode
    {
        public int PlayerCount { get; }
        public List<PlayerManager> _players;
        private GameEngine _engine;
        public string ModeName => "Single Player";
        public void Initialize()
        {
            _engine = new GameEngine();

            foreach (PlayerManager player in _players)
            {
                (player.Chart, player.ScoreSet)= new JsonLoader().LoadFromJson($"{SongManager.CurrentSong}_{player.Difficulty}.json");
                player.Score.SetScoreSet(player.ScoreSet);
                foreach (Note note in player.Chart)
                {
                    note.X = Hyperparameters.SpawnZoneXCoordinate;
                    note.Y = Hyperparameters.SinglePlayerYCoordinate;
                    note.SpawnTime = note.HitTime - (Hyperparameters.Length / player.Speed);
                }
            }
            _engine.StartGame();
            // Additional initialization for single player mode can be added here
        }
        public SinglePlayerMode()
        {
            PlayerCount = 1;
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
