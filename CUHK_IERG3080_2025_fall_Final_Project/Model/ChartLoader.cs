using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public static class ChartLoader
    {
        public static List<Note> LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Chart file not found.", filePath);
            }

            string json = File.ReadAllText(filePath);
            return ParseNotes(json);
        }

        private static List<Note> ParseNotes(string json)
        {
            var notes = new List<Note>();
            var notesArrayMatch = Regex.Match(json, @"""notes""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
            if (!notesArrayMatch.Success)
            {
                return notes;
            }
            string notesContent = notesArrayMatch.Groups[1].Value;

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

            return notes.OrderBy(n => n.HitTime).ToList();
        }

        private static Note.NoteType ParseNoteType(string type)
        {
            return type.ToLower() == "red" ? Note.NoteType.Red : Note.NoteType.Blue;
        }
    }
}
