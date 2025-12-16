using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class GameOverVM : ViewModelBase
    {
        // Score properties for display
        public int TotalScore { get; private set; }
        public int PerfectHits { get; private set; }
        public int GoodHits { get; private set; }
        public int BadHits { get; private set; }
        public int MissHits { get; private set; }
        public int MaxCombo { get; private set; }
        public int TotalNotes { get; private set; }

        // Grade properties
        public string Grade { get; private set; }
        public string GradeColor { get; private set; }

        // Accuracy percentage
        public string Accuracy { get; private set; }

        public GameOverVM()
        {
            LoadScoreData();
            CalculateGrade();
        }

        private void LoadScoreData()
        {
            // Get score data from the first player in current mode
            if (GameModeManager.CurrentMode != null &&
                GameModeManager.CurrentMode.Players != null &&
                GameModeManager.CurrentMode.Players.Count > 0)
            {
                var player = GameModeManager.CurrentMode.Players[0];
                var score = player.Score;

                TotalScore = score.Score;
                PerfectHits = score.PerfectHit;
                GoodHits = score.GoodHit;
                BadHits = score.BadHit;
                MissHits = score.MissHit;
                MaxCombo = score.MaxCombo;

                // Calculate total notes
                TotalNotes = PerfectHits + GoodHits + BadHits + MissHits;

                // Calculate accuracy
                if (TotalNotes > 0)
                {
                    double accuracyPercent = (double)PerfectHits / TotalNotes * 100;
                    Accuracy = $"{accuracyPercent:F1}%";
                }
                else
                {
                    Accuracy = "0.0%";
                }
            }
            else
            {
                // Default values if no data available
                TotalScore = 0;
                PerfectHits = 0;
                GoodHits = 0;
                BadHits = 0;
                MissHits = 0;
                MaxCombo = 0;
                TotalNotes = 0;
                Accuracy = "0.0%";
            }

            // Notify UI of all property changes
            OnPropertyChanged(nameof(TotalScore));
            OnPropertyChanged(nameof(PerfectHits));
            OnPropertyChanged(nameof(GoodHits));
            OnPropertyChanged(nameof(BadHits));
            OnPropertyChanged(nameof(MissHits));
            OnPropertyChanged(nameof(MaxCombo));
            OnPropertyChanged(nameof(TotalNotes));
            OnPropertyChanged(nameof(Accuracy));
        }

        private void CalculateGrade()
        {
            if (TotalNotes == 0)
            {
                Grade = "F";
                GradeColor = "#FF808080"; // Gray
                OnPropertyChanged(nameof(Grade));
                OnPropertyChanged(nameof(GradeColor));
                return;
            }

            // Calculate perfect ratio
            double perfectRatio = (double)PerfectHits / TotalNotes;

            // Grade system based on perfect hits ratio
            if (perfectRatio >= 0.95)
            {
                Grade = "S";
                GradeColor = "#FFD4AF37"; // Gold (matching button style)
            }
            else if (perfectRatio >= 0.90)
            {
                Grade = "A";
                GradeColor = "#FFFFE08A"; // Bright gold
            }
            else if (perfectRatio >= 0.80)
            {
                Grade = "B";
                GradeColor = "#FF3B82F6"; // Lighter blue
            }
            else if (perfectRatio >= 0.70)
            {
                Grade = "C";
                GradeColor = "#FF1E3A8A"; // Deep blue
            }
            else if (perfectRatio >= 0.60)
            {
                Grade = "D";
                GradeColor = "#FF6B7280"; // Gray-blue
            }
            else
            {
                Grade = "F";
                GradeColor = "#FF991B1B"; // Red
            }

            OnPropertyChanged(nameof(Grade));
            OnPropertyChanged(nameof(GradeColor));
        }
    }
}