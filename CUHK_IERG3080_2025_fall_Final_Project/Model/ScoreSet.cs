using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class ScoreSet
    {
        public int PerfectHitScore { get; set; }
        public int GoodHitScore { get; set; }
        public int BadHitScore { get; set; }
        public int MissHitScore { get; set; }
        public ScoreSet()
        {
            PerfectHitScore = Hyperparameters.DefaultPerfectScore;
            GoodHitScore = Hyperparameters.DefaultGoodScore;
            BadHitScore = Hyperparameters.DefaultBadScore;
            MissHitScore = Hyperparameters.DefaultMissScore;
        }
    }
}
