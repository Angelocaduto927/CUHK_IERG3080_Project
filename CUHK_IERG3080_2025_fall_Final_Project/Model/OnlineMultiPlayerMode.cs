using System;
using System.Collections.Generic;
using System.IO;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class OnlineMultiPlayerMode : IGameMode
    {
        public int PlayerCount { get; }
        public List<PlayerManager> _players;
        public string ModeName => "Online Multi Player";

        public OnlineMultiPlayerMode()
        {
            PlayerCount = 2;
            _players = new List<PlayerManager>();
            CreatePlayers();
        }

        public void CreatePlayers()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerManager player = new PlayerManager(playerIndex: i, isLocalPlayer: false);

                // ✅ 兜底：避免 difficulty 为空导致 Initialize() 崩
                if (player?.Difficulty != null && string.IsNullOrWhiteSpace(player.Difficulty.CurrentDifficulty))
                    player.Difficulty.CurrentDifficulty = "Easy";

                _players.Add(player);
            }
        }

        public void Initialize()
        {
            // ✅ 再兜一次（防止 Joiner 进 InGame 时还没同步到）
            for (int i = 0; i < _players.Count; i++)
            {
                var p = _players[i];
                if (p?.Difficulty != null && string.IsNullOrWhiteSpace(p.Difficulty.CurrentDifficulty))
                    p.Difficulty.CurrentDifficulty = "Easy";
            }

            foreach (PlayerManager player in _players)
            {
                string diff = player?.Difficulty?.CurrentDifficulty;
                if (string.IsNullOrWhiteSpace(diff))
                    diff = "Easy";

                string song = SongManager.CurrentSong;
                if (string.IsNullOrWhiteSpace(song))
                    song = Hyperparameters.Song1Name; // 最终兜底（理论上 SongSelection 会设置）

                string chartPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Chart",
                    $"{song}_{diff}.json");

                if (!File.Exists(chartPath))
                {
                    // 让报错更可读：直接告诉你它要找哪个文件
                    throw new FileNotFoundException("Chart file not found: " + chartPath, chartPath);
                }

                (player.Chart, player.ScoreSet) = new JsonLoader().LoadFromJson(chartPath);
                player.Score.SetScoreSet(player.ScoreSet);
                player.noteManager.Set_allNotes(player.Chart);

                double laneY = (player.PlayerIndex == 0)
                    ? Hyperparameters.MultiPlayerUpperYCoordinate
                    : Hyperparameters.MultiPlayerLowerYCoordinate;

                foreach (Note note in player.Chart)
                {
                    note.X = Hyperparameters.SpawnZoneXCoordinate;
                    note.Y = laneY;
                    note.Speed = player.Speed;
                    note.SpawnTime = note.HitTime - (Hyperparameters.Length / note.Speed);
                }
            }
        }
    }
}
