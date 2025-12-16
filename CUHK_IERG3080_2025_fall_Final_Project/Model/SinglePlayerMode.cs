using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class SinglePlayerMode : IGameMode
    {
        private int PlayerCount;
        public List<PlayerManager> _players;
        private GameEngine _engine;
        public string ModeName => "Single Player";
        public void Initialize()
        {
            _engine = new GameEngine();
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
                PlayerManager player = new PlayerManager(i + 1);
                _players.Add(player);
            }
        }
    }
}
