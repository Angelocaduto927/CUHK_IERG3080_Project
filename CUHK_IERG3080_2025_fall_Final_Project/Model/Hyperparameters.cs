using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public static class Hyperparameters
    {
        //Hit Window in milliseconds
        public const double PerfectWindow = 50;
        public const double GoodWindow = 100;
        public const double BadWindow = 150;
        public const double MissWindow = 200;
        
        //Coordinates
        public const double SinglePlayerYCoordinate = 400;
        public const double MultiPlayerUpperYCoordinate = 500;
        public const double MultiPlayerLowerYCoordinate = 300;
        public const double HitZoneXCoordinate = 0;
        public const double SpawnZoneXCoordinate = 400;

        //Size
        public const double Length = SpawnZoneXCoordinate - HitZoneXCoordinate;

        //Speed
        public const double DefaultSpeed = 1;

        //Score
        public const int DefaultPerfectScore = 300;
        public const int DefaultGoodScore = 200;
        public const int DefaultBadScore = 100;
        public const int DefaultMissScore = 0;
    }
}
