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
        public const double BandWidth = 150;
        public const double EllipseSize = 60;
        public const double LineDistance = 270;
        public const double SinglePlayerYCoordinate = 200;
        // VV must be > bandwidth
        public const double MultiPlayerUpperYCoordinate = 200;
        public const double MultiPlayerLowerYCoordinate = 200;
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

        //Accuracy Calculation Weight
        public const double PerfectWeight = 1.0;
        public const double GoodWeight = 0.9;
        public const double BadWeight = 0.6;
        public const double MissWeight = 0.0;

        //SongName
        public const string Song1Name = "One_Last_Kiss";
        public const string Song2Name = "Song2";
    }
}
