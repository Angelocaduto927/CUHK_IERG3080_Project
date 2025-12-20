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

        private int _disconnecting = 0; // ✅ single-flight guard

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

            // ✅ Read loop in background; completion triggers disconnect cleanup (no UI block)
            _readTask = _codec.ReadLoopAsync(HandleIncomingAsync, _cts.Token);
            _readTask.ContinueWith(t =>
            {
                // observe faults
                try { _ = t.Exception; } catch { }

                // EOF / network error -> disconnect without sending Abort
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
            // JoinOk
            if (msg is JoinOkMsg ok)
            {
                Log("[Joiner] JoinOk: slot=" + ok.Slot + " room=" + ok.RoomId);
                OnJoinOk?.Invoke(ok);
                return Task.CompletedTask;
            }

            // JoinReject
            if (msg is JoinRejectMsg rej)
            {
                Log("[Joiner] JoinRejected: " + rej.Reason);
                OnJoinRejected?.Invoke(rej.Reason);

                // ✅ server already decided, just disconnect (no Abort back)
                _ = DisconnectAsync("Join rejected: " + rej.Reason, sendAbort: false);
                return Task.CompletedTask;
            }

            // Abort from remote
            if (msg is AbortMsg ab)
            {
                Log("[Joiner] Abort: " + ab.Reason);

                // ✅ 关键：收到 Abort 不要回发 Abort（避免乒乓/重入）
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

            // ✅ 先把字段置空，避免重入时继续用旧对象
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

                // ✅ 关闭 socket 让 ReadLineAsync 立刻退出
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
                // 不要恢复 _disconnecting，避免同一次对象被重复 disconnect
            }
        }

        private void CleanupSocket()
        {
            try { _client?.Close(); } catch { }
            _client = null;
        }

        private void Log(string s) => OnLog?.Invoke(s);

        // ✅ 关键：Dispose 不允许阻塞 UI 线程
        public void Dispose()
        {
            _ = DisconnectAsync("Dispose", sendAbort: false);
        }
    }
}
