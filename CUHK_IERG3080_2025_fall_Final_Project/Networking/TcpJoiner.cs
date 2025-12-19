using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Networking
{
    public sealed class TcpJoiner : IDisposable
    {
        private TcpClient _client;
        private NetCodec _codec;

        private CancellationTokenSource _cts;
        private Task _readTask;

        public bool IsConnected { get { return _client != null && _client.Connected; } }

        public string RoomId { get; private set; } = "room1";
        public int LocalSlot { get; private set; } = 0;

        public event Action<string> OnLog;
        public event Action<JoinOkMsg> OnJoinOk;
        public event Action<string> OnJoinRejected;
        public event Action OnDisconnected;
        public event Action<NetMsg> OnMessage;

        public async Task ConnectAndJoinAsync(string hostIp, int port, string playerName)
        {
            if (_client != null) return;

            _cts = new CancellationTokenSource();

            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(hostIp, port).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CleanupSocket();
                throw new Exception("Connect failed: " + ex.Message);
            }

            _codec = new NetCodec(_client.GetStream());
            Log("[Joiner] Connected to " + hostIp + ":" + port + ", sending Join...");

            _readTask = _codec.ReadLoopAsync(async msg =>
            {
                await HandleIncomingAsync(msg).ConfigureAwait(false);
            }, _cts.Token);

            await _codec.SendAsync(new JoinMsg { Name = playerName }, _cts.Token).ConfigureAwait(false);
        }

        public Task SendAsync(NetMsg msg)
        {
            if (_codec == null || _cts == null) return Task.CompletedTask;
            return _codec.SendAsync(msg, _cts.Token);
        }

        private Task HandleIncomingAsync(NetMsg msg)
        {
            var ok = msg as JoinOkMsg;
            if (ok != null)
            {
                RoomId = ok.RoomId;
                LocalSlot = ok.Slot;
                Log("[Joiner] JoinOk: slot=" + ok.Slot + " msg=" + ok.Message);
                if (OnJoinOk != null) OnJoinOk(ok);
                return Task.CompletedTask;
            }

            var rej = msg as JoinRejectMsg;
            if (rej != null)
            {
                Log("[Joiner] JoinRejected: " + rej.Reason);
                if (OnJoinRejected != null) OnJoinRejected(rej.Reason);
                _ = DisconnectAsync("Join rejected");
                return Task.CompletedTask;
            }

            var ab = msg as AbortMsg;
            if (ab != null)
            {
                Log("[Joiner] Abort: " + ab.Reason);
                _ = DisconnectAsync(ab.Reason);
                return Task.CompletedTask;
            }

            if (OnMessage != null) OnMessage(msg);
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason)
        {
            if (_cts == null) return;

            try
            {
                if (_codec != null)
                    await _codec.SendAsync(new AbortMsg { Reason = reason }, _cts.Token).ConfigureAwait(false);
            }
            catch { }

            try { _cts.Cancel(); } catch { }

            CleanupSocket();

            if (_readTask != null)
            {
                try { await _readTask.ConfigureAwait(false); } catch { }
                _readTask = null;
            }

            try { _cts.Dispose(); } catch { }
            _cts = null;

            if (OnDisconnected != null) OnDisconnected();
        }

        private void CleanupSocket()
        {
            try { if (_client != null) _client.Close(); } catch { }
            _client = null;
            _codec = null;
        }

        private void Log(string s)
        {
            if (OnLog != null) OnLog(s);
        }

        public void Dispose()
        {
            try { DisconnectAsync("Dispose").GetAwaiter().GetResult(); } catch { }
        }
    }
}
