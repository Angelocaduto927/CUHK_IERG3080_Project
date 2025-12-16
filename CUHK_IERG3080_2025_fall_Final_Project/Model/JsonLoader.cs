using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    internal class JsonLoader
    {
        public List<Note> Chart;
        public ScoreSet ScoreSet;
        public JsonLoader() { }
        public void LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Chart file not found.", filePath);
            }
            string json = File.ReadAllText(filePath);
            (Chart, ScoreSet) = ParseJson(json);
        }
        public List<Note> GetChart()
        {
            return Chart;
        }
        public ScoreSet GetScoreSet()
        {
            return ScoreSet;
        }
        private static (List<Note>, ScoreSet) ParseJson(string json)
        {
            var notes = new List<Note>();
            var scoreSet = new ScoreSet();
            var notesArrayMatch = Regex.Match(json, @"""notes""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
            var scoreSetMatch = Regex.Match(json, @"""scoresets""\s*:\s*\{(.*?)\}", RegexOptions.Singleline);
            if (!notesArrayMatch.Success || !scoreSetMatch.Success)
            {
                return (notes,scoreSet);
            }
            string notesContent = notesArrayMatch.Groups[1].Value;
            string scoreSetContent = scoreSetMatch.Groups[1].Value;

            // 匹配每个 note 对象： { "timestamp": 2000, "type": "Don" }
            var noteMatches = Regex.Matches(notesContent, @"\{[^}]+\}");

            foreach (Match noteMatch in noteMatches)
            {
                string noteJson = noteMatch.Value;

                // 提取 timestamp
                var timestampMatch = Regex.Match(noteJson, @"""timestamp""\s*:\s*(\d+\.?\d*)");
                if (!timestampMatch.Success) continue;
                double timestamp = double.Parse(timestampMatch.Groups[1].Value);

                // 提取 type
                var typeMatch = Regex.Match(noteJson, @"""type""\s*:\s*""(\w+)""");
                if (!typeMatch.Success) continue;
                string type = typeMatch.Groups[1].Value;

                notes.Add(new Note
                {
                    HitTime = timestamp,
                    Type = ParseNoteType(type),
                });
            }
            ParseScoreSet(scoreSetContent, scoreSet);
            return (notes.OrderBy(n => n.HitTime).ToList(), scoreSet);
        }
        private static Note.NoteType ParseNoteType(string type)
        {
            return type.ToLower() == "red" ? Note.NoteType.Red : Note.NoteType.Blue;
        }
        private static void ParseScoreSet(string scoreSetContent, ScoreSet scoreSet)
        {
            var perfectHitMatch = Regex.Match(scoreSetContent, @"""PerfectHitScore""\s*:\s*(\d+)");
            var goodHitMatch = Regex.Match(scoreSetContent, @"""GoodHitScore""\s*:\s*(\d+)");
            var badHitMatch = Regex.Match(scoreSetContent, @"""BadHitScore""\s*:\s*(\d+)");
            var missHitMatch = Regex.Match(scoreSetContent, @"""MissHitScore""\s*:\s*(\d+)");
            if (perfectHitMatch.Success)
            {
                scoreSet.PerfectHitScore = int.Parse(perfectHitMatch.Groups[1].Value);
            }
            if (goodHitMatch.Success)
            {
                scoreSet.GoodHitScore = int.Parse(goodHitMatch.Groups[1].Value);
            }
            if (badHitMatch.Success)
            {
                scoreSet.BadHitScore = int.Parse(badHitMatch.Groups[1].Value);
            }
            if (missHitMatch.Success)
            {
                scoreSet.MissHitScore = int.Parse(missHitMatch.Groups[1].Value);
            }
        }
    }
}
