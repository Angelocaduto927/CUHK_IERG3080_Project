using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class SinglePlayerMode : IGameMode
    {
        private int PlayerCount = 1;
        private List<PlayerManager> _players = new List<PlayerManager>();
        private GameEngine _engine;
        public string ModeName => "Single Player";
        public void Initialize(GameEngine engine)
        {
            _engine = engine;
            // Additional initialization for single player mode can be added here
        }
        public SinglePlayerMode()
        {
            
        }
    }
}
