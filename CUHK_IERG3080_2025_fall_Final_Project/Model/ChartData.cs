using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class ChartData
    {
        public string SongName { get; set; }
        public int BPM { get; set; }
        public List<NoteData> Notes { get; set; }
    }

    public class NoteData
    {
        public double Timestamp { get; set; }
        public string Type { get; set; }
    }
}
