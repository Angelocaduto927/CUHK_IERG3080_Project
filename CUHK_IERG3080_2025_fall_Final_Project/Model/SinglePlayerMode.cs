using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class SinglePlayerMode : IGameMode
    {
        private GameEngine _engine;
        private PlayerManager _player;
        public string ModeName => "Single Player";
        public void Initialize(GameEngine engine)
        {
            _engine = engine;
            _player = new PlayerManager(0);
            // Additional initialization for single player mode can be added here
        }

    }
}
