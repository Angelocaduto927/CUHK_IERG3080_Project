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

        private int _disconnecting = 0;

        public bool IsConnected => _client != null && _client.Connected;

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
                try { _cts?.Dispose(); } catch { }
                _cts = null;
                throw new Exception("Connect failed: " + ex.Message);
            }

            _codec = new NetCodec(_client.GetStream());

            Log("[Joiner] Connected to " + hostIp + ":" + port + ", sending Join...");

            _readTask = _codec.ReadLoopAsync(HandleIncomingAsync, _cts.Token);
            _readTask.ContinueWith(t =>
            {
                try { _ = t.Exception; } catch { }

                _ = DisconnectAsync("Disconnected", sendAbort: false);
            }, TaskScheduler.Default);

            await _codec.SendAsync(new JoinMsg { Name = playerName }, _cts.Token).ConfigureAwait(false);
        }

        public Task SendAsync(NetMsg msg)
        {
            if (_codec == null || _cts == null) return Task.CompletedTask;
            return _codec.SendAsync(msg, _cts.Token);
        }

        private Task HandleIncomingAsync(NetMsg msg)
        {
            if (msg is JoinOkMsg ok)
            {
                Log("[Joiner] JoinOk: slot=" + ok.Slot + " room=" + ok.RoomId);
                OnJoinOk?.Invoke(ok);
                return Task.CompletedTask;
            }

            if (msg is JoinRejectMsg rej)
            {
                Log("[Joiner] JoinRejected: " + rej.Reason);
                OnJoinRejected?.Invoke(rej.Reason);

                _ = DisconnectAsync("Join rejected: " + rej.Reason, sendAbort: false);
                return Task.CompletedTask;
            }

            if (msg is AbortMsg ab)
            {
                Log("[Joiner] Abort: " + ab.Reason);

                _ = DisconnectAsync("Remote abort: " + ab.Reason, sendAbort: false);
                return Task.CompletedTask;
            }

            OnMessage?.Invoke(msg);
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(string reason, bool sendAbort = true)
        {
            if (Interlocked.Exchange(ref _disconnecting, 1) == 1) return;

            var cts = _cts;
            var codec = _codec;

            _cts = null;
            _codec = null;

            try
            {
                try
                {
                    if (sendAbort && codec != null && cts != null && !cts.IsCancellationRequested)
                        await codec.SendAsync(new AbortMsg { Reason = reason }, cts.Token).ConfigureAwait(false);
                }
                catch { }

                try { cts?.Cancel(); } catch { }

                CleanupSocket();

                if (_readTask != null)
                {
                    try { await _readTask.ConfigureAwait(false); } catch { }
                    _readTask = null;
                }

                try { cts?.Dispose(); } catch { }

                OnDisconnected?.Invoke();
            }
            finally
            {
            }
        }

        private void CleanupSocket()
        {
            try { _client?.Close(); } catch { }
            _client = null;
        }

        private void Log(string s) => OnLog?.Invoke(s);

        public void Dispose()
        {
            _ = DisconnectAsync("Dispose", sendAbort: false);
        }
    }
}
