using System;
using System.Collections;
using System.Reflection;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class GameOverVM : ViewModelBase
    {
        private readonly Action _navigateToTitleScreen;
        public int TotalScore { get; private set; }
        public int PerfectHits { get; private set; }
        public int GoodHits { get; private set; }
        public int BadHits { get; private set; }
        public int MissHits { get; private set; }
        public int MaxCombo { get; private set; }
        public int TotalNotes { get; private set; }
        public string Grade { get; private set; }
        public string GradeColor { get; private set; }
        public string Accuracy { get; private set; }

        public GameOverVM(Action navigateToTitleScreen)
        {
            _navigateToTitleScreen = navigateToTitleScreen;

            LoadScoreData();
            CalculateGrade();
        }

        private void LoadScoreData()
        {
            // Get score data from the first player in current mode  
            var players = GetCurrentModePlayers();
            if (players != null && players.Count > 0)
            {
                dynamic player = players[0];
                dynamic score = player?.Score;

                if (score != null)
                {
                    TotalScore = (int)score.Score;
                    PerfectHits = (int)score.PerfectHit;
                    GoodHits = (int)score.GoodHit;
                    BadHits = (int)score.BadHit;
                    MissHits = (int)score.MissHit;
                    MaxCombo = (int)score.MaxCombo;

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
            }
            else
            {
                SetDefaultValues();
            }

            OnPropertyChanged(nameof(TotalScore));
            OnPropertyChanged(nameof(PerfectHits));
            OnPropertyChanged(nameof(GoodHits));
            OnPropertyChanged(nameof(BadHits));
            OnPropertyChanged(nameof(MissHits));
            OnPropertyChanged(nameof(MaxCombo));
            OnPropertyChanged(nameof(TotalNotes));
            OnPropertyChanged(nameof(Accuracy));
        }

        private void SetDefaultValues()
        {
            TotalScore = 0;
            PerfectHits = 0;
            GoodHits = 0;
            BadHits = 0;
            MissHits = 0;
            MaxCombo = 0;
            TotalNotes = 0;
            Accuracy = "0.0%";
        }

        private void CalculateGrade()
        {
            if (TotalNotes == 0)
            {
                Grade = "F";
                GradeColor = "#FF808080";
                OnPropertyChanged(nameof(Grade));
                OnPropertyChanged(nameof(GradeColor));
                return;
            }

            double perfectRatio = (double)PerfectHits / TotalNotes;

            if (perfectRatio >= 0.95)
            {
                Grade = "S";
                GradeColor = "#FFD4AF37";
            }
            else if (perfectRatio >= 0.90)
            {
                Grade = "A";
                GradeColor = "#FFFFE08A";
            }
            else if (perfectRatio >= 0.80)
            {
                Grade = "B";
                GradeColor = "#FF3B82F6";
            }
            else if (perfectRatio >= 0.70)
            {
                Grade = "C";
                GradeColor = "#FF1E3A8A";
            }
            else if (perfectRatio >= 0.60)
            {
                Grade = "D";
                GradeColor = "#FF6B7280";
            }
            else
            {
                Grade = "F";
                GradeColor = "#FF991B1B";
            }

            OnPropertyChanged(nameof(Grade));
            OnPropertyChanged(nameof(GradeColor));
        }

        private IList GetCurrentModePlayers()
        {
            var mode = GameModeManager.CurrentMode;
            if (mode == null)
                return null;

            var field = mode.GetType().GetField("_players");
            return field?.GetValue(mode) as IList;
        }
    }
}
