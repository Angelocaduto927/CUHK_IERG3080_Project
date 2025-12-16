using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public interface IGameMode
    {
        // In a mode, it is a complete game. So need to have players, song, difficulty, 
        string ModeName { get; };
        int PlayerCount { get; set; };
        void CreatePlayers();
        void Initialize(GameEngine engine);
    }
}
