using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public interface IGameMode
    {
        string ModeName { get; }
        void Initialize(GameEngine engine);

    }
}
