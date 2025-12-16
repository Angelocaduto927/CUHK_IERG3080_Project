using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class OnlineMultiPlayerMode : IGameMode
    {
        public int PlayerCount { get; }
        public List<PlayerManager> _players;
        private GameEngine _engine;
        public string ModeName => "Online Multi Player";
        public void Initialize()
        {
            _engine = new GameEngine();
            // Additional initialization for online multiplayer mode can be added here
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
                PlayerManager player = new PlayerManager(playerIndex: i + 1, isLocalPlayer: false);
                _players.Add(player);
            }
        }
    }
}
