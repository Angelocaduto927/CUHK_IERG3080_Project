using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CUHK_IERG3080_2025_fall_Final_Project.Networking
{
    public sealed class NetCodec
    {
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);

        public NetCodec(Stream stream)
        {
            _reader = new StreamReader(stream, new UTF8Encoding(false), false, 8192, true);
            _writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, true)
            {
                AutoFlush = true,
                NewLine = "\n"
            };
        }

        public async Task SendAsync(NetMsg msg, CancellationToken ct)
        {
            string line = _json.Serialize(msg);

            await _writeGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await _writer.WriteLineAsync(line).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task ReadLoopAsync(Func<NetMsg, Task> onMsg, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                string line;
                try
                {
                    line = await _reader.ReadLineAsync().ConfigureAwait(false);
                }
                catch
                {
                    break;
                }

                if (line == null) break;     // EOF
                if (line.Length == 0) continue;

                NetMsg msg;
                try
                {
                    msg = Deserialize(line);
                }
                catch
                {
                    continue;
                }

                if (msg != null)
                    await onMsg(msg).ConfigureAwait(false);
            }
        }

        private NetMsg Deserialize(string line)
        {
            var baseMsg = _json.Deserialize<NetMsg>(line);
            if (baseMsg == null || string.IsNullOrWhiteSpace(baseMsg.Type))
                throw new InvalidDataException("Missing Type.");

            switch (baseMsg.Type)
            {
                case MsgType.Join: return _json.Deserialize<JoinMsg>(line);
                case MsgType.JoinOk: return _json.Deserialize<JoinOkMsg>(line);
                case MsgType.JoinReject: return _json.Deserialize<JoinRejectMsg>(line);

                case MsgType.SelectSong: return _json.Deserialize<SelectSongMsg>(line);
                case MsgType.Start: return _json.Deserialize<StartMsg>(line);
                case MsgType.Input: return _json.Deserialize<InputMsg>(line);

                case MsgType.Abort: return _json.Deserialize<AbortMsg>(line);
                case MsgType.System: return _json.Deserialize<SystemMsg>(line);


                case MsgType.SelectDifficulty: return _json.Deserialize<SelectDifficultyMsg>(line);
                case MsgType.Ready: return _json.Deserialize<ReadyMsg>(line);


                default:
                    return baseMsg;
            }
        }
    }
}
