using CUHK_IERG3080_2025_fall_Final_Project.Model;
using CUHK_IERG3080_2025_fall_Final_Project.Networking;
using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using System;
using System.Collections;
using System.Reflection;
using System.Windows;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class GameOverVM : ViewModelBase
    {
        private readonly Action _navigateToTitleScreen;
        private readonly OnlineSession _session;

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

            // ✅ Online 时：GameOver 优先读 Session 的最终结算缓存
            _session = (GameModeManager.CurrentMode is OnlineMultiPlayerMode) ? GameModeManager.OnlineSession : null;

            if (_session != null)
            {
                _session.OnMatchSummary += _ =>
                {
                    // 网络回调可能不在 UI 线程
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        LoadScoreData();
                    }));
                };
            }

            LoadScoreData();
        }

        private void LoadScoreData()
        {
            var players = GetCurrentModePlayers();

            // ✅ 1) Online：如果拿得到最终结算，就用最终结算（最权威）
            if (TryLoadFromOnlineSummary(players))
            {
                NotifyAll();
                return;
            }

            // ✅ 2) fallback：原来逻辑（本地 _players[i].Score）
            LoadFromLocalPlayers(players);
            NotifyAll();
        }

        private bool TryLoadFromOnlineSummary(IList players)
        {
            if (_session == null) return false;

            var s1 = _session.LastSummarySlot1;
            var s2 = _session.LastSummarySlot2;

            if (s1 == null && s2 == null) return false;

            // 名字 fallback：优先 Summary，其次 players
            string p1NameFallback = (players != null && players.Count > 0) ? (players[0] as dynamic)?.PlayerName : "Player 1";
            string p2NameFallback = (players != null && players.Count > 1) ? (players[1] as dynamic)?.PlayerName : "Player 2";

            ApplySummaryToPlayer(1, s1, p1NameFallback);
            ApplySummaryToPlayer(2, s2, p2NameFallback);

            Player2Visible = (s2 != null) || (players != null && players.Count > 1);
            return true;
        }

        private void ApplySummaryToPlayer(int idx, MatchSummaryMsg s, string nameFallback)
        {
            if (s == null)
            {
                SetDefaultValuesForPlayer(idx);
                CalculateGradeForPlayer(idx);
                if (idx == 1) Player1Name = nameFallback ?? "Player 1";
                else Player2Name = nameFallback ?? "Player 2";
                return;
            }

            if (idx == 1)
            {
                Player1Name = string.IsNullOrWhiteSpace(s.PlayerName) ? (nameFallback ?? "Player 1") : s.PlayerName;
                TotalScore1 = s.Score;
                PerfectHits1 = s.PerfectHit;
                GoodHits1 = s.GoodHit;
                BadHits1 = s.BadHit;
                MissHits1 = s.MissHit;
                MaxCombo1 = s.MaxCombo;
                TotalNotes1 = (s.TotalNotes > 0) ? s.TotalNotes : (PerfectHits1 + GoodHits1 + BadHits1 + MissHits1);
                Accuracy1 = $"{s.Accuracy:F1}%";
                CalculateGradeForPlayer(1);
            }
            else
            {
                Player2Name = string.IsNullOrWhiteSpace(s.PlayerName) ? (nameFallback ?? "Player 2") : s.PlayerName;
                TotalScore2 = s.Score;
                PerfectHits2 = s.PerfectHit;
                GoodHits2 = s.GoodHit;
                BadHits2 = s.BadHit;
                MissHits2 = s.MissHit;
                MaxCombo2 = s.MaxCombo;
                TotalNotes2 = (s.TotalNotes > 0) ? s.TotalNotes : (PerfectHits2 + GoodHits2 + BadHits2 + MissHits2);
                Accuracy2 = $"{s.Accuracy:F1}%";
                CalculateGradeForPlayer(2);
            }
        }

        private void LoadFromLocalPlayers(IList players)
        {
            if (players != null && players.Count > 0)
            {
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
        }

        private void NotifyAll()
        {
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
            int total = playerIdx == 1 ? TotalNotes1 : TotalNotes2;
            int totalscore = playerIdx == 1 ? TotalScore1 : TotalScore2;

            string grade;
            string color;

            if (total == 0)
            {
                grade = "F";
                color = "#FF808080";
            }
            else
            {
                if (totalscore >= 96000) { grade = "S"; color = "#FFD4AF37"; }
                else if (totalscore >= 92000) { grade = "A"; color = "#FFFFE08A"; }
                else if (totalscore >= 82000) { grade = "B"; color = "#FF3B82F6"; }
                else if (totalscore >= 72000) { grade = "C"; color = "#FF1E3A8A"; }
                else if (totalscore >= 60000) { grade = "D"; color = "#FF6B7280"; }
                else { grade = "F"; color = "#FF991B1B"; }
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
