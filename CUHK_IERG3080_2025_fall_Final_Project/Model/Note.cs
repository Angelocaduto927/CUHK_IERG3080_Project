using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly List<Note> _activeNotes;
        private readonly PlayerManager _player;

        private int _nextSpawnIndex = 0;

        public IReadOnlyList<Note> ActiveNotes => _activeNotes;

        public bool HasPendingNotes => _nextSpawnIndex < _allNotes.Count || _activeNotes.Count > 0;

        public NoteManager(List<Note> notes, PlayerManager player)
        {
            _allNotes = (notes ?? new List<Note>()).OrderBy(n => n.SpawnTime).ToList();
            _activeNotes = new List<Note>();
            _player = player;
            _nextSpawnIndex = 0;
        }

        public void Set_allNotes(List<Note> notes)
        {
            _allNotes = (notes ?? new List<Note>()).OrderBy(n => n.SpawnTime).ToList();
            Reset();
        }

        public void Update(double currentTime)
        {
            SpawnNotes(currentTime);
            UpdateNotePositions(currentTime);
            CheckMissedNotes(currentTime);
        }

        private void SpawnNotes(double currentTime)
        {
            while (_nextSpawnIndex < _allNotes.Count && _allNotes[_nextSpawnIndex].SpawnTime <= currentTime)
            {
                var note = _allNotes[_nextSpawnIndex++];
                if (note.State == Note.NoteState.NotSpawned)
                {
                    note.State = Note.NoteState.Active;
                    _activeNotes.Add(note);
                }
            }
        }

        public void UpdateNotePositions(double currentTime)
        {
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                _activeNotes[i].UpdatePosition(currentTime);
            }
        }

        private void CheckMissedNotes(double currentTime)
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                if (note.IsMissed(currentTime))
                {
                    note.State = Note.NoteState.Missed;
                    _activeNotes.RemoveAt(i);
                }
            }
        }

        public void HitNote(double currentTime, Note.NoteType noteType)
        {
            var targetNote = _activeNotes
                .Where(n => n.Type == noteType)
                .OrderBy(n => Math.Abs(n.HitTime - currentTime))
                .FirstOrDefault();

            if (targetNote == null) return;

            double timeDifference = Math.Abs(targetNote.HitTime - currentTime);

            HitResult? result = null;
            if (timeDifference <= Hyperparameters.PerfectWindow) result = HitResult.Perfect;
            else if (timeDifference <= Hyperparameters.GoodWindow) result = HitResult.Good;
            else if (timeDifference <= Hyperparameters.BadWindow) result = HitResult.Bad;
            else if (timeDifference <= Hyperparameters.MissWindow) result = HitResult.Miss;
            else return;

            targetNote.State = Note.NoteState.Hit;
            _activeNotes.Remove(targetNote);

            bool isEarly = currentTime < targetNote.HitTime;
            bool isLate = currentTime > targetNote.HitTime;
            _player.Score.Update(result.Value, isEarly, isLate);
        }

        public void Reset()
        {
            _activeNotes.Clear();

            _nextSpawnIndex = 0;

            if (_allNotes != null)
            {
                foreach (var note in _allNotes)
                {
                    note.State = Note.NoteState.NotSpawned;
                }
            }
        }
    }
}
