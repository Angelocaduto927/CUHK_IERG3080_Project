using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Networking
{
    public sealed class TcpHost : IDisposable
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetCodec _codec;

        private CancellationTokenSource _cts;
        private Task _acceptTask;
        private Task _readTask;

        public int Port { get; private set; }
        public bool IsRunning { get { return _listener != null; } }
        public bool HasClient { get { return _client != null && _client.Connected; } }

        public string RoomId { get; set; } = "room1";

        public event Action<string> OnLog;
        public event Action<string> OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action<NetMsg> OnMessage;

        public Task StartAsync(int port)
        {
            if (_listener != null) return Task.CompletedTask;

            Port = port;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Log("[Host] Listening on 0.0.0.0:" + port);

            _acceptTask = AcceptLoopAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_listener == null) return;

            try { _cts.Cancel(); } catch { }

            try { if (_client != null) _client.Close(); } catch { }
            try { _listener.Stop(); } catch { }

            _client = null;
            _codec = null;
            _listener = null;

            if (_acceptTask != null) { try { await _acceptTask; } catch { } _acceptTask = null; }
            if (_readTask != null) { try { await _readTask; } catch { } _readTask = null; }

            try { _cts.Dispose(); } catch { }
            _cts = null;

            Log("[Host] Stopped");
        }

        public Task SendAsync(NetMsg msg)
        {
            if (_codec == null || _cts == null) return Task.CompletedTask;
            return _codec.SendAsync(msg, _cts.Token);
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                TcpClient incoming = null;
                try { incoming = await _listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                catch { break; }

                if (ct.IsCancellationRequested) break;

                if (_client != null)
                {
                    try
                    {
                        var tempCodec = new NetCodec(incoming.GetStream());
                        await tempCodec.SendAsync(new JoinRejectMsg { Reason = "Host already has a player connected." }, ct)
                            .ConfigureAwait(false);
                    }
                    catch { }
                    try { incoming.Close(); } catch { }
                    continue;
                }

                _client = incoming;
                _codec = new NetCodec(_client.GetStream());

                Log("[Host] Client connected, waiting for Join...");

                _readTask = _codec.ReadLoopAsync(async msg => { await HandleIncomingAsync(msg).ConfigureAwait(false); }, ct);
            }
        }

        private async Task HandleIncomingAsync(NetMsg msg)
        {
            var join = msg as JoinMsg;
            if (join != null)
            {
                Log("[Host] Join from: " + join.Name);

                await SendAsync(new JoinOkMsg
                {
                    Slot = 2,
                    RoomId = RoomId,
                    Message = "Welcome " + join.Name + ", you are Player 2."
                }).ConfigureAwait(false);

                if (OnClientConnected != null) OnClientConnected(join.Name);
                return;
            }

            var ab = msg as AbortMsg;
            if (ab != null)
            {
                Log("[Host] Client abort: " + ab.Reason);
                DropClient();
                return;
            }

            if (OnMessage != null) OnMessage(msg);
        }

        private void DropClient()
        {
            try { if (_client != null) _client.Close(); } catch { }
            _client = null;
            _codec = null;
            Log("[Host] Client disconnected");
            if (OnClientDisconnected != null) OnClientDisconnected();
        }

        private void Log(string s)
        {
            if (OnLog != null) OnLog(s);
        }

        public void Dispose()
        {
            try { StopAsync().GetAwaiter().GetResult(); } catch { }
        }
    }
}
