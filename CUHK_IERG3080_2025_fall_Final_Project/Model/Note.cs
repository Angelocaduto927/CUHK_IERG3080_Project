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

        // ✅ CHANGED: 用 spawn 指针，避免每帧 Where/ToList
        private int _nextSpawnIndex = 0;

        // ✅ CHANGED: List 本身就实现 IReadOnlyList，避免每帧 AsReadOnly 产生包装开销
        public IReadOnlyList<Note> ActiveNotes => _activeNotes;

        // ✅ CHANGED: O(1) 判断是否还有未生成/正在飞的 note（给 GameEngine.IsGameFinished 用）
        public bool HasPendingNotes => _nextSpawnIndex < _allNotes.Count || _activeNotes.Count > 0;

        public NoteManager(List<Note> notes, PlayerManager player)
        {
            // ✅ CHANGED: 保证按 SpawnTime 排序，spawn 指针才能正确推进
            _allNotes = (notes ?? new List<Note>()).OrderBy(n => n.SpawnTime).ToList();
            _activeNotes = new List<Note>();
            _player = player;
            _nextSpawnIndex = 0;
        }

        // ✅ CHANGED: 设置新谱面时要排序 + reset 指针
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

        // ✅ CHANGED: 用 while + 指针推进，替代 Where(...).ToList()
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

        // ✅ CHANGED: 倒序 RemoveAt，替代 Where(...).ToList() 再 Remove
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
            // 这段不是每帧跑（只在按键时），保留 LINQ 问题不大
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

            // ✅ CHANGED: reset spawn 指针
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
