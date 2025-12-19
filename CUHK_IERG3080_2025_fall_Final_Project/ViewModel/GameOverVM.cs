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

        // Player 1
        public string Player1Name { get; private set; }
        public int TotalScore1 { get; private set; }
        public int PerfectHits1 { get; private set; }
        public int GoodHits1 { get; private set; }
        public int BadHits1 { get; private set; }
        public int MissHits1 { get; private set; }
        public int MaxCombo1 { get; private set; }
        public int TotalNotes1 { get; private set; }
        public string Grade1 { get; private set; }
        public string GradeColor1 { get; private set; }
        public string Accuracy1 { get; private set; }

        // Player 2
        public string Player2Name { get; private set; }
        public int TotalScore2 { get; private set; }
        public int PerfectHits2 { get; private set; }
        public int GoodHits2 { get; private set; }
        public int BadHits2 { get; private set; }
        public int MissHits2 { get; private set; }
        public int MaxCombo2 { get; private set; }
        public int TotalNotes2 { get; private set; }
        public string Grade2 { get; private set; }
        public string GradeColor2 { get; private set; }
        public string Accuracy2 { get; private set; }

        public bool Player2Visible { get; private set; }

        public GameOverVM(Action navigateToTitleScreen)
        {
            _navigateToTitleScreen = navigateToTitleScreen;

            LoadScoreData();
        }

        private void LoadScoreData()
        {
            var players = GetCurrentModePlayers();
            if (players != null && players.Count > 0)
            {
                // Player 1
                dynamic player1 = players[0];
                dynamic score1 = player1?.Score;
                Player1Name = player1?.PlayerName ?? "Player 1";

                if (score1 != null)
                {
                    TotalScore1 = (int)score1.Score;
                    PerfectHits1 = (int)score1.PerfectHit;
                    GoodHits1 = (int)score1.GoodHit;
                    BadHits1 = (int)score1.BadHit;
                    MissHits1 = (int)score1.MissHit;
                    MaxCombo1 = (int)score1.MaxCombo;
                    TotalNotes1 = PerfectHits1 + GoodHits1 + BadHits1 + MissHits1;
                    Accuracy1 = TotalNotes1 > 0 ? $"{((double)PerfectHits1 / TotalNotes1 * 100):F1}%" : "0.0%";
                }
                else
                {
                    SetDefaultValuesForPlayer(1);
                }

                CalculateGradeForPlayer(1);
            }
            else
            {
                SetDefaultValuesForPlayer(1);
                CalculateGradeForPlayer(1);
                Player1Name = "Player 1";
            }

            // Player 2 (optional)
            if (players != null && players.Count > 1)
            {
                Player2Visible = true;
                dynamic player2 = players[1];
                dynamic score2 = player2?.Score;
                Player2Name = player2?.PlayerName ?? "Player 2";

                if (score2 != null)
                {
                    TotalScore2 = (int)score2.Score;
                    PerfectHits2 = (int)score2.PerfectHit;
                    GoodHits2 = (int)score2.GoodHit;
                    BadHits2 = (int)score2.BadHit;
                    MissHits2 = (int)score2.MissHit;
                    MaxCombo2 = (int)score2.MaxCombo;
                    TotalNotes2 = PerfectHits2 + GoodHits2 + BadHits2 + MissHits2;
                    Accuracy2 = TotalNotes2 > 0 ? $"{((double)PerfectHits2 / TotalNotes2 * 100):F1}%" : "0.0%";
                }
                else
                {
                    SetDefaultValuesForPlayer(2);
                }

                CalculateGradeForPlayer(2);
            }
            else
            {
                Player2Visible = false;
                SetDefaultValuesForPlayer(2);
                CalculateGradeForPlayer(2);
                Player2Name = "Player 2";
            }

            // Notify bindings
            OnPropertyChanged(nameof(Player1Name));
            OnPropertyChanged(nameof(TotalScore1));
            OnPropertyChanged(nameof(PerfectHits1));
            OnPropertyChanged(nameof(GoodHits1));
            OnPropertyChanged(nameof(BadHits1));
            OnPropertyChanged(nameof(MissHits1));
            OnPropertyChanged(nameof(MaxCombo1));
            OnPropertyChanged(nameof(TotalNotes1));
            OnPropertyChanged(nameof(Accuracy1));
            OnPropertyChanged(nameof(Grade1));
            OnPropertyChanged(nameof(GradeColor1));

            OnPropertyChanged(nameof(Player2Name));
            OnPropertyChanged(nameof(TotalScore2));
            OnPropertyChanged(nameof(PerfectHits2));
            OnPropertyChanged(nameof(GoodHits2));
            OnPropertyChanged(nameof(BadHits2));
            OnPropertyChanged(nameof(MissHits2));
            OnPropertyChanged(nameof(MaxCombo2));
            OnPropertyChanged(nameof(TotalNotes2));
            OnPropertyChanged(nameof(Accuracy2));
            OnPropertyChanged(nameof(Grade2));
            OnPropertyChanged(nameof(GradeColor2));

            OnPropertyChanged(nameof(Player2Visible));
        }

        private void SetDefaultValuesForPlayer(int playerIdx)
        {
            if (playerIdx == 1)
            {
                TotalScore1 = 0;
                PerfectHits1 = 0;
                GoodHits1 = 0;
                BadHits1 = 0;
                MissHits1 = 0;
                MaxCombo1 = 0;
                TotalNotes1 = 0;
                Accuracy1 = "0.0%";
            }
            else
            {
                TotalScore2 = 0;
                PerfectHits2 = 0;
                GoodHits2 = 0;
                BadHits2 = 0;
                MissHits2 = 0;
                MaxCombo2 = 0;
                TotalNotes2 = 0;
                Accuracy2 = "0.0%";
            }
        }

        private void CalculateGradeForPlayer(int playerIdx)
        {
            int perfect = playerIdx == 1 ? PerfectHits1 : PerfectHits2;
            int total = playerIdx == 1 ? TotalNotes1 : TotalNotes2;

            string grade;
            string color;

            if (total == 0)
            {
                grade = "F";
                color = "#FF808080";
            }
            else
            {
                double perfectRatio = (double)perfect / total;
                if (perfectRatio >= 0.95)
                {
                    grade = "S";
                    color = "#FFD4AF37";
                }
                else if (perfectRatio >= 0.90)
                {
                    grade = "A";
                    color = "#FFFFE08A";
                }
                else if (perfectRatio >= 0.80)
                {
                    grade = "B";
                    color = "#FF3B82F6";
                }
                else if (perfectRatio >= 0.70)
                {
                    grade = "C";
                    color = "#FF1E3A8A";
                }
                else if (perfectRatio >= 0.60)
                {
                    grade = "D";
                    color = "#FF6B7280";
                }
                else
                {
                    grade = "F";
                    color = "#FF991B1B";
                }
            }

            if (playerIdx == 1)
            {
                Grade1 = grade;
                GradeColor1 = color;
                OnPropertyChanged(nameof(Grade1));
                OnPropertyChanged(nameof(GradeColor1));
            }
            else
            {
                Grade2 = grade;
                GradeColor2 = color;
                OnPropertyChanged(nameof(Grade2));
                OnPropertyChanged(nameof(GradeColor2));
            }
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
