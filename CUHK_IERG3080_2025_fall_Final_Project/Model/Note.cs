using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    internal class Note
    {
        public enum NoteType { Red, Blue }
        public NoteType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Size { get; set; }
        public double HitTime { get; set; }

    }
}
