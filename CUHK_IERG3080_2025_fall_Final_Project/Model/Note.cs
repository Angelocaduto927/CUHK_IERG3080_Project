using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class Note
    {
        public enum NoteType { Red, Blue }
        public enum NoteState { NotSpawned, Active, Hit, Missed }
        public NoteType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double HitTime { get; set; }
        public double SpawnTime { get; set; }
        public NoteState State { get; set; }
        public const double PerfectHitWindow = Hyperparameters.PerfectWindow;
        public const double GoodHitWindow = Hyperparameters.GoodWindow;
        public const double BadHitWindow = Hyperparameters.BadWindow;
        public const double MissHitWindow = Hyperparameters.MissWindow;
        public Note()
        {
            State = NoteState.NotSpawned;
        }
        public void UpdatePosition(double currentTime, double speed)
        {

        }

    }
}
