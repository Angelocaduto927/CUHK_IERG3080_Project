using System;

namespace CUHK_IERG3080_2025_fall_Final_Project.Shared
{
    public static class MsgType
    {
        public const string Join = "Join";
        public const string JoinOk = "JoinOk";
        public const string JoinReject = "JoinReject";

        public const string Start = "Start";
        public const string Input = "Input";
        public const string SelectSong = "SelectSong";

        public const string Abort = "Abort";
        public const string Ping = "Ping";
        public const string Pong = "Pong";
        public const string System = "System";

        public const string SelectDifficulty = "SelectDifficulty";
        public const string Ready = "Ready";

        public const string HitResult = "HitResult";

        public const string MatchSummary = "MatchSummary";
        public const string UpdatePlayerSetting = "UpdatePlayerSetting";
    }

    [Serializable]
    public class NetMsg
    {
        public string Type { get; set; } = "";
    }

    [Serializable]
    public sealed class JoinMsg : NetMsg
    {
        public string Name { get; set; } = "Player";
        public JoinMsg() { Type = MsgType.Join; }
    }

    [Serializable]
    public sealed class JoinOkMsg : NetMsg
    {
        public int Slot { get; set; }
        public string RoomId { get; set; } = "room1";
        public string Message { get; set; } = "";
        public JoinOkMsg() { Type = MsgType.JoinOk; }
    }

    [Serializable]
    public sealed class JoinRejectMsg : NetMsg
    {
        public string Reason { get; set; } = "Room is full";
        public JoinRejectMsg() { Type = MsgType.JoinReject; }
    }

    [Serializable]
    public sealed class StartMsg : NetMsg
    {
        public int StartInMs { get; set; } = 2000;
        public long StartAtUnixMs { get; set; } = 0;
        public StartMsg() { Type = MsgType.Start; }
    }

    [Serializable]
    public sealed class SelectSongMsg : NetMsg
    {
        public string SongId { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public SelectSongMsg() { Type = MsgType.SelectSong; }
    }

    [Serializable]
    public sealed class InputMsg : NetMsg
    {
        public int Slot { get; set; }
        public string NoteType { get; set; } = "Red";
        public double AtMs { get; set; }
        public InputMsg() { Type = MsgType.Input; }
    }

    [Serializable]
    public sealed class HitResultMsg : NetMsg
    {
        public int Slot { get; set; }
        public string NoteType { get; set; } = "Red";
        public double AtMs { get; set; }
        public string Result { get; set; } = "Tap";

        public int Score { get; set; } = 0;
        public int Combo { get; set; } = 0;
        public double Accuracy { get; set; } = 100;

        public HitResultMsg() { Type = MsgType.HitResult; }
    }

    [Serializable]
    public sealed class AbortMsg : NetMsg
    {
        public string Reason { get; set; } = "Disconnected";
        public AbortMsg() { Type = MsgType.Abort; }
    }

    [Serializable]
    public sealed class SystemMsg : NetMsg
    {
        public string Text { get; set; } = "";
        public SystemMsg() { Type = MsgType.System; }
    }

    [Serializable]
    public sealed class SelectDifficultyMsg : NetMsg
    {
        public int Slot { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public SelectDifficultyMsg() { Type = MsgType.SelectDifficulty; }
    }

    [Serializable]
    public sealed class ReadyMsg : NetMsg
    {
        public int Slot { get; set; }
        public bool IsReady { get; set; } = true;
        public ReadyMsg() { Type = MsgType.Ready; }
    }

    [Serializable]
    public sealed class MatchSummaryMsg : NetMsg
    {
        public int Slot { get; set; } = 1;
        public string PlayerName { get; set; } = "";

        public int Score { get; set; } = 0;
        public int PerfectHit { get; set; } = 0;
        public int GoodHit { get; set; } = 0;
        public int BadHit { get; set; } = 0;
        public int MissHit { get; set; } = 0;

        public int MaxCombo { get; set; } = 0;
        public int TotalNotes { get; set; } = 0;
        public double Accuracy { get; set; } = 0;

        public MatchSummaryMsg() { Type = MsgType.MatchSummary; }
    }

    public sealed class UpdatePlayerSettingMsg : NetMsg
    {
        public int Slot { get; set; } = 1;
        public double Speed { get; set; } = 1.0;
        public UpdatePlayerSettingMsg() { Type = MsgType.UpdatePlayerSetting; }
    }
}
