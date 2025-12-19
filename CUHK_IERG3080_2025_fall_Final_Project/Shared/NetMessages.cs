using System;

namespace CUHK_IERG3080_2025_fall_Final_Project.Shared
{
    // === 消息类型枚举（用字符串也行，这里统一用常量避免拼写错）===
    public static class MsgType
    {
        public const string Join = "Join";             // Client -> Host
        public const string JoinOk = "JoinOk";         // Host -> Client
        public const string JoinReject = "JoinReject"; // Host -> Client

        public const string Start = "Start";           // Host -> Both (或 Host->Client, Host 本地直接触发)
        public const string Input = "Input";           // Both directions
        public const string SelectSong = "SelectSong"; // Host -> Client

        public const string Abort = "Abort";           // Host/Client -> other (断线/退出/错误)
        public const string Ping = "Ping";             // optional
        public const string Pong = "Pong";             // optional
        public const string System = "System";         // optional log

        public const string SelectDifficulty = "SelectDifficulty";
        public const string Ready = "Ready";

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
        public int Slot { get; set; }          // 1 or 2（你是 P1 还是 P2）
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

    // === Start ===（第一版用相对延迟，简单稳定）
    [Serializable]
    public sealed class StartMsg : NetMsg
    {
        public int StartInMs { get; set; } = 2000;

        // 新增：绝对开始时间（UTC Unix ms），用于“同时开始”
        public long StartAtUnixMs { get; set; } = 0;

        public StartMsg() { Type = MsgType.Start; }
    }


    // === 选歌同步（房主发）===
    [Serializable]
    public sealed class SelectSongMsg : NetMsg
    {
        public string SongId { get; set; } = "";      // 例如 "IRIS_OUT"
        public string Difficulty { get; set; } = "";  // 例如 "Easy"/"Hard"
        public SelectSongMsg() { Type = MsgType.SelectSong; }
    }

    // === 输入同步（核心）===
    // AtMs：使用“游戏引擎时间轴 ms”（你 GameEngine.CurrentTime 就是 ms）
    // NoteType：用字符串 "Red"/"Blue"
    [Serializable]
    public sealed class InputMsg : NetMsg
    {
        public int Slot { get; set; }           // 谁按的：1 or 2
        public string NoteType { get; set; } = "Red";
        public double AtMs { get; set; }        // ms on engine timeline
        public InputMsg() { Type = MsgType.Input; }
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
        public string Difficulty { get; set; } = "Easy"; // "Easy"/"Hard"
        public SelectDifficultyMsg() { Type = MsgType.SelectDifficulty; }
    }

    [Serializable]
    public sealed class ReadyMsg : NetMsg
    {
        public int Slot { get; set; }        // 1 or 2
        public bool IsReady { get; set; } = true;
        public ReadyMsg() { Type = MsgType.Ready; }
    }

}
