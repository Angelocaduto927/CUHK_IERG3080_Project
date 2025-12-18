using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace CUHK_IERG3080_2025_fall_Final_Project.Model
{
    public class Note
    {
        public enum NoteType { Red, Blue }
        public enum NoteState { NotSpawned, Active, Hit, Missed }
        public NoteType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double HitTime { get; set; }
        public double SpawnTime { get; set; }
        public double Speed { get; set; }
        public NoteState State { get; set; }
        /*
        public const double PerfectHitWindow = Hyperparameters.PerfectWindow;
        public const double GoodHitWindow = Hyperparameters.GoodWindow;
        public const double BadHitWindow = Hyperparameters.BadWindow;
        public const double MissHitWindow = Hyperparameters.MissWindow;
        */
        public Note()
        {
            State = NoteState.NotSpawned;
        }
        public bool ShouldSpawn(double currentTime)
        {
            return State == NoteState.NotSpawned && currentTime >= SpawnTime;
        }
        public bool IsMissed(double currentTime)
        {
            return State == NoteState.Active && currentTime > HitTime + Hyperparameters.MissWindow;
        }
        public void UpdatePosition(double currentTime)
        {
            if (State == NoteState.Active)
            {
                X = Hyperparameters.SpawnZoneXCoordinate - (currentTime - SpawnTime) * Speed;
            }
        }
    }

    public class NoteManager
    {
        private List<Note> _allNotes;
        private List<Note> _activeNotes;
        private PlayerManager _player;
        public IReadOnlyList<Note> ActiveNotes => _activeNotes.AsReadOnly();
        public NoteManager(List<Note> notes, PlayerManager player)
        {
            _allNotes = notes;
            _activeNotes = new List<Note>();
            _player = player;
        }
        public void Set_allNotes(List<Note> notes)
        {
            _allNotes = notes;
        }
        public void Update(double currentTime)
        {
            SpawnNotes(currentTime);
            UpdateNotePositions(currentTime);
            CheckMissedNotes(currentTime);
        }
        private void SpawnNotes(double currentTime)
        {
            var notesToSpawn = _allNotes.Where(n => n.ShouldSpawn(currentTime)).ToList();
            foreach (var note in notesToSpawn)
            {
                note.State = Note.NoteState.Active;
               _activeNotes.Add(note);
            }

        }
        public void UpdateNotePositions(double currentTime)
        {
            foreach (var note in _activeNotes)
            {
                note.UpdatePosition(currentTime);
            }
        }
        private void CheckMissedNotes(double currentTime)
        {
            var missedNotes = _activeNotes.Where(n => n.IsMissed(currentTime)).ToList();
            foreach (var note in missedNotes)
            {
                note.State = Note.NoteState.Missed;
                _activeNotes.Remove(note);
            }
        }
        public void HitNote(double currentTime, Note.NoteType noteType)
        {
            var targetNote = _activeNotes
                .Where(n => n.Type == noteType)
                .OrderBy(n => Math.Abs(n.HitTime - currentTime))
                .FirstOrDefault();
            if (targetNote == null)
            {
                return;
            }
            double timeDifference = Math.Abs(targetNote.HitTime - currentTime);
            HitResult? result = null;
            if (timeDifference <= Hyperparameters.PerfectWindow)
            {
                result = HitResult.Perfect;
            }
            else if (timeDifference <= Hyperparameters.GoodWindow)
            {
                result = HitResult.Good;
            }
            else if (timeDifference <= Hyperparameters.BadWindow)
            {
                result = HitResult.Bad;
            }
            else if (timeDifference <= Hyperparameters.MissWindow)
            {
                result = HitResult.Miss;
            }
            else 
            {
                return;
            }
            targetNote.State = Note.NoteState.Hit;
            _activeNotes.Remove(targetNote);

            bool isEarly = currentTime < targetNote.HitTime;
            bool isLate = currentTime > targetNote.HitTime;
            _player.Score.Update(result.Value, isEarly, isLate);
        }
        public void Reset()
        {
            _activeNotes.Clear();
            foreach (var note in _allNotes)
            {
                note.State = Note.NoteState.NotSpawned;
            }
        }
    }
}
