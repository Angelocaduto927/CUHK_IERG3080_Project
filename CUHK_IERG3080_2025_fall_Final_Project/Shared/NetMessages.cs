using System;

namespace CUHK_IERG3080_2025_fall_Final_Project.Shared
{
    // === 消息类型枚举（用字符串也行，这里统一用常量避免拼写错）===
    public static class MsgType
    {
        public const string Join = "Join";             // Client -> Host
        public const string JoinOk = "JoinOk";         // Host -> Client
        public const string JoinReject = "JoinReject"; // Host -> Client

        public const string Start = "Start";           // Host -> Both
        public const string Input = "Input";           // Both directions
        public const string SelectSong = "SelectSong"; // Host -> Client

        public const string Abort = "Abort";           // Host/Client -> other
        public const string Ping = "Ping";             // optional
        public const string Pong = "Pong";             // optional
        public const string System = "System";         // optional log

        public const string SelectDifficulty = "SelectDifficulty";
        public const string Ready = "Ready";

        // ✅ 新增：击打结果（权威分数/判定/效果）
        public const string HitResult = "HitResult";

        public const string MatchSummary = "MatchSummary";
        public const string UpdatePlayerSetting = "UpdatePlayerSetting";
    }

    // === 基类 ===
    [Serializable]
    public class NetMsg
    {
        public string Type { get; set; } = "";
    }

    // === Join ===
    [Serializable]
    public sealed class JoinMsg : NetMsg
    {
        public string Name { get; set; } = "Player";
        public JoinMsg() { Type = MsgType.Join; }
    }

    [Serializable]
    public sealed class JoinOkMsg : NetMsg
    {
        public int Slot { get; set; }          // 1 or 2
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

    // === Start ===
    [Serializable]
    public sealed class StartMsg : NetMsg
    {
        public int StartInMs { get; set; } = 2000;
        public long StartAtUnixMs { get; set; } = 0;
        public StartMsg() { Type = MsgType.Start; }
    }

    // === 选歌同步（房主发）===
    [Serializable]
    public sealed class SelectSongMsg : NetMsg
    {
        public string SongId { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public SelectSongMsg() { Type = MsgType.SelectSong; }
    }

    // === 输入同步（驱动对方轨道 note 消失/推进用）===
    [Serializable]
    public sealed class InputMsg : NetMsg
    {
        public int Slot { get; set; }           // 1 or 2
        public string NoteType { get; set; } = "Red";
        public double AtMs { get; set; }        // ms on engine timeline
        public InputMsg() { Type = MsgType.Input; }
    }

    // ✅ 新增：击打结果（由击打者本机计算完发送）
    // Result: "Perfect"/"Good"/"Bad"/"Miss"/"Tap"
    [Serializable]
    public sealed class HitResultMsg : NetMsg
    {
        public int Slot { get; set; }               // 谁打的：1/2
        public string NoteType { get; set; } = "Red";
        public double AtMs { get; set; }            // 引擎时间轴 ms（用于对齐效果）
        public string Result { get; set; } = "Tap"; // Perfect/Good/Bad/Miss/Tap

        // 权威显示值（对方端不再本地判定）
        public int Score { get; set; } = 0;
        public int Combo { get; set; } = 0;
        public double Accuracy { get; set; } = 100;

        public HitResultMsg() { Type = MsgType.HitResult; }
    }

    // === Abort / 断线 / 错误 ===
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
        public int Slot { get; set; }        // 1 or 2
        public string Difficulty { get; set; } = "Easy";
        public SelectDifficultyMsg() { Type = MsgType.SelectDifficulty; }
    }

    [Serializable]
    public sealed class ReadyMsg : NetMsg
    {
        public int Slot { get; set; }        // 1 or 2
        public bool IsReady { get; set; } = true;
        public ReadyMsg() { Type = MsgType.Ready; }
    }

    [Serializable]
    public sealed class MatchSummaryMsg : NetMsg
    {
        public int Slot { get; set; } = 1;          // 1 or 2
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
        public int Slot { get; set; } = 1;          // 1 or 2
        public double Speed { get; set; } = 1.0;
        public UpdatePlayerSettingMsg() { Type = MsgType.UpdatePlayerSetting; }
    }
}
