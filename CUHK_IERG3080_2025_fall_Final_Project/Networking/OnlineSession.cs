using CUHK_IERG3080_2025_fall_Final_Project.Shared;
using System;
using System.Linq;
using System.Net; // ✅ add
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CUHK_IERG3080_2025_fall_Final_Project.Networking
{
    public sealed class OnlineSession : IDisposable
    {
        private TcpHost _host;
        private TcpJoiner _joiner;

        private int _shuttingDown = 0;

        // =========================
        // ✅ Heartbeat / disconnect detection
        // =========================
        private CancellationTokenSource _hbCts;
        private Task _hbTask;
        private long _lastRxUtcMs = 0;
        private int _disconnectNotified = 0;

        private const int HeartbeatIntervalMs = 2000;
        private const int HeartbeatTimeoutMs = 8000;
        private const string PingText = "__PING__";
        private const string PongText = "__PONG__";

        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }

        public int LocalSlot { get; private set; } = 0;
        public string LocalName { get; private set; } = "Player";
        public string RoomId { get; private set; } = "room1";
        public int Port { get; private set; } = 5050;

        // ✅ 缓存：Joiner 晚订阅也不会丢 Start / 选歌
        public StartMsg LastStartMsg { get; private set; }
        public SelectSongMsg LastSelectSongMsg { get; private set; }

        // ✅ 房间状态缓存
        public string Slot1Difficulty { get; private set; }
        public string Slot2Difficulty { get; private set; }
        public bool Slot1Ready { get; private set; }
        public bool Slot2Ready { get; private set; }

        // ✅ GameOver 结算缓存（关键：Shutdown 不清空）
        public MatchSummaryMsg LastSummarySlot1 { get; private set; }
        public MatchSummaryMsg LastSummarySlot2 { get; private set; }

        // ✅ 只提供一个可分享地址（不再显示 localhost，也不列出多网卡）
        public string[] ShareAddresses
        {
            get
            {
                var ips = GetLanIPv4Addresses();
                var best = ChooseBestShareIp(ips);

                // 没拿到 LAN IP（极少数情况，比如无网卡/权限/奇怪环境），才 fallback
                if (string.IsNullOrWhiteSpace(best))
                    best = "127.0.0.1";

                return new[] { $"{best}:{Port}" };
            }
        }

        public event Action<string> OnLog;
        public event Action<int> OnConnected;
        public event Action<string> OnDisconnected;

        public event Action<SelectSongMsg> OnSelectSong;
        public event Action<SelectDifficultyMsg> OnSelectDifficulty;
        public event Action<ReadyMsg> OnReady;

        public event Action<StartMsg> OnStart;
        public event Action<InputMsg> OnInput;

        // ✅ 实时权威判定/分数（InGame 显示用）
        public event Action<HitResultMsg> OnHitResult;

        // ✅ 最终结算（GameOver 用）
        public event Action<MatchSummaryMsg> OnMatchSummary;

        // =========================
        // Heartbeat helpers
        // =========================
        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void ResetDisconnectGuard()
        {
            Interlocked.Exchange(ref _disconnectNotified, 0);
        }

        private void MarkRx()
        {
            Interlocked.Exchange(ref _lastRxUtcMs, NowMs());
        }

        private void NotifyDisconnectedOnce(string reason)
        {
            if (Interlocked.Exchange(ref _disconnectNotified, 1) == 1) return;
            OnDisconnected?.Invoke(reason);
        }

        private void StopHeartbeat()
        {
            try { _hbCts?.Cancel(); } catch { }
            try { _hbCts?.Dispose(); } catch { }
            _hbCts = null;
            _hbTask = null;
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();

            // 新连接：允许再次触发断线提示
            ResetDisconnectGuard();

            MarkRx();

            _hbCts = new CancellationTokenSource();
            var ct = _hbCts.Token;

            _hbTask = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try { await Task.Delay(HeartbeatIntervalMs, ct).ConfigureAwait(false); }
                    catch { break; }

                    if (ct.IsCancellationRequested) break;
                    if (!IsConnected) break;

                    long age = NowMs() - Interlocked.Read(ref _lastRxUtcMs);
                    if (age > HeartbeatTimeoutMs)
                    {
                        await ForceDisconnectAsync("Connection timeout").ConfigureAwait(false);
                        break;
                    }

                    try
                    {
                        // 发送 ping（对方会回 pong，双方都能刷新 lastRx）
                        await SendAsync(new SystemMsg { Text = PingText }).ConfigureAwait(false);
                    }
                    catch
                    {
                        await ForceDisconnectAsync("Connection lost").ConfigureAwait(false);
                        break;
                    }
                }
            }, ct);
        }

        private async Task ForceDisconnectAsync(string reason)
        {
            StopHeartbeat();

            try { await ShutdownAsync(reason).ConfigureAwait(false); } catch { }

            IsConnected = false;
            NotifyDisconnectedOnce(reason);
        }

        // =========================
        // Host / Join
        // =========================
        public async Task StartHostAsync(int port, string name, string roomId = "room1")
        {
            await ShutdownAsync("Restart host").ConfigureAwait(false);

            // 新 session：允许再次触发断线提示
            ResetDisconnectGuard();

            // ✅ 新开房/新局：清结算
            LastSummarySlot1 = null;
            LastSummarySlot2 = null;

            Port = port;
            LocalName = name;
            RoomId = roomId;

            IsHost = true;
            LocalSlot = 1;
            IsConnected = false;

            _host = new TcpHost { RoomId = roomId };
            _host.OnLog += Log;

            _host.OnClientConnected += clientName =>
            {
                IsConnected = true;
                Log("[Session] Client '" + clientName + "' connected.");
                OnConnected?.Invoke(LocalSlot);

                // ✅ 连接建立后开始心跳检测（用于断网/进程被关的断联检测）
                StartHeartbeat();

                // ✅ Joiner 连上后，把当前状态补发（避免不同步）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (LastSelectSongMsg != null)
                            await _host.SendAsync(LastSelectSongMsg).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(Slot1Difficulty))
                            await _host.SendAsync(new SelectDifficultyMsg { Slot = 1, Difficulty = Slot1Difficulty }).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(Slot2Difficulty))
                            await _host.SendAsync(new SelectDifficultyMsg { Slot = 2, Difficulty = Slot2Difficulty }).ConfigureAwait(false);

                        await _host.SendAsync(new ReadyMsg { Slot = 1, IsReady = Slot1Ready }).ConfigureAwait(false);
                        await _host.SendAsync(new ReadyMsg { Slot = 2, IsReady = Slot2Ready }).ConfigureAwait(false);

                        // ✅ 如果已经有结算，也补发（可选）
                        if (LastSummarySlot1 != null) await _host.SendAsync(LastSummarySlot1).ConfigureAwait(false);
                        if (LastSummarySlot2 != null) await _host.SendAsync(LastSummarySlot2).ConfigureAwait(false);
                    }
                    catch { }
                });
            };

            _host.OnClientDisconnected += () =>
            {
                IsConnected = false;
                _ = ForceDisconnectAsync("Client disconnected");
            };

            _host.OnMessage += HandleIncoming;

            await _host.StartAsync(port).ConfigureAwait(false);

            Log("[Session] Host started.");
            Log("[Session] Share address:");
            foreach (var a in ShareAddresses) Log("  " + a);
        }

        public async Task JoinHostAsync(string hostIp, int port, string name)
        {
            await ShutdownAsync("Restart join").ConfigureAwait(false);

            // 新 session：允许再次触发断线提示
            ResetDisconnectGuard();

            // ✅ 新连接：清结算
            LastSummarySlot1 = null;
            LastSummarySlot2 = null;

            Port = port;
            LocalName = name;

            IsHost = false;
            IsConnected = false;
            LocalSlot = 0;

            _joiner = new TcpJoiner();
            _joiner.OnLog += Log;

            _joiner.OnJoinOk += ok =>
            {
                LocalSlot = ok.Slot;
                RoomId = ok.RoomId;
                IsConnected = true;
                OnConnected?.Invoke(LocalSlot);

                // ✅ Join 成功后开始心跳
                StartHeartbeat();
            };

            _joiner.OnJoinRejected += reason =>
            {
                IsConnected = false;
                StopHeartbeat();
                NotifyDisconnectedOnce("Join rejected: " + reason);
            };

            _joiner.OnDisconnected += () =>
            {
                IsConnected = false;
                _ = ForceDisconnectAsync("Disconnected");
            };

            _joiner.OnMessage += HandleIncoming;

            await _joiner.ConnectAndJoinAsync(hostIp, port, name).ConfigureAwait(false);
        }

        // =========================
        // Lobby sync
        // =========================
        public Task SendSelectSongAsync(string songId, string difficulty)
        {
            var msg = new SelectSongMsg { SongId = songId, Difficulty = difficulty };
            LastSelectSongMsg = msg;

            // 如果你这里是全局难度，也可同步写入
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                Slot1Difficulty = difficulty;
                Slot2Difficulty = difficulty;
            }

            return SendAsync(msg);
        }

        public Task SendSelectDifficultyAsync(int slot, string difficulty)
        {
            if (slot == 1) Slot1Difficulty = difficulty;
            if (slot == 2) Slot2Difficulty = difficulty;

            return SendAsync(new SelectDifficultyMsg { Slot = slot, Difficulty = difficulty });
        }

        public Task SendReadyAsync(int slot, bool isReady)
        {
            if (slot == 1) Slot1Ready = isReady;
            if (slot == 2) Slot2Ready = isReady;

            return SendAsync(new ReadyMsg { Slot = slot, IsReady = isReady });
        }

        // =========================
        // Game sync
        // =========================
        public Task SendStartAsync(int startInMs = 1500)
        {
            long startAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + startInMs;
            var msg = new StartMsg { StartInMs = startInMs, StartAtUnixMs = startAt };

            LastStartMsg = msg;
            OnStart?.Invoke(msg);

            return SendAsync(msg);
        }

        public Task SendInputAsync(int slot, string noteType, double atMs)
        {
            return SendAsync(new InputMsg { Slot = slot, NoteType = noteType, AtMs = atMs });
        }

        public Task SendHitResultAsync(int slot, string noteType, double atMs, string result, int score, int combo, double accuracy)
        {
            return SendAsync(new HitResultMsg
            {
                Slot = slot,
                NoteType = noteType ?? "Red",
                AtMs = atMs,
                Result = string.IsNullOrWhiteSpace(result) ? "Tap" : result,
                Score = score,
                Combo = combo,
                Accuracy = accuracy
            });
        }

        // ✅ GameOver：发送最终结算（本地先缓存）
        public Task SendMatchSummaryAsync(MatchSummaryMsg msg)
        {
            CacheSummary(msg);
            return SendAsync(msg);
        }

        private void CacheSummary(MatchSummaryMsg msg)
        {
            if (msg == null) return;
            if (msg.Slot == 1) LastSummarySlot1 = msg;
            else if (msg.Slot == 2) LastSummarySlot2 = msg;
        }

        // =========================
        // Disconnect
        // =========================
        public async Task LeaveAsync(string reason = "Leave")
        {
            try { await SendAsync(new AbortMsg { Reason = reason }).ConfigureAwait(false); } catch { }
            StopHeartbeat();
            await ShutdownAsync(reason).ConfigureAwait(false);
            NotifyDisconnectedOnce(reason);
        }

        // =========================
        // Internal send / receive
        // =========================
        private Task SendAsync(NetMsg msg)
        {
            if (IsHost)
                return _host != null ? _host.SendAsync(msg) : Task.CompletedTask;
            else
                return _joiner != null ? _joiner.SendAsync(msg) : Task.CompletedTask;
        }

        private void HandleIncoming(NetMsg msg)
        {
            if (msg != null) MarkRx();

            if (msg is SelectSongMsg sel)
            {
                LastSelectSongMsg = sel;

                if (!string.IsNullOrWhiteSpace(sel.Difficulty))
                {
                    Slot1Difficulty = sel.Difficulty;
                    Slot2Difficulty = sel.Difficulty;
                }

                OnSelectSong?.Invoke(sel);
                return;
            }

            if (msg is SelectDifficultyMsg dif)
            {
                if (dif.Slot == 1) Slot1Difficulty = dif.Difficulty;
                if (dif.Slot == 2) Slot2Difficulty = dif.Difficulty;

                OnSelectDifficulty?.Invoke(dif);
                return;
            }

            if (msg is ReadyMsg r)
            {
                if (r.Slot == 1) Slot1Ready = r.IsReady;
                if (r.Slot == 2) Slot2Ready = r.IsReady;

                OnReady?.Invoke(r);
                return;
            }

            if (msg is StartMsg st)
            {
                LastStartMsg = st;
                OnStart?.Invoke(st);
                return;
            }

            if (msg is InputMsg inp)
            {
                OnInput?.Invoke(inp);
                return;
            }

            if (msg is HitResultMsg hr)
            {
                OnHitResult?.Invoke(hr);
                return;
            }

            if (msg is MatchSummaryMsg sum)
            {
                CacheSummary(sum);
                OnMatchSummary?.Invoke(sum);
                return;
            }

            if (msg is AbortMsg ab)
            {
                IsConnected = false;
                _ = ForceDisconnectAsync(string.IsNullOrWhiteSpace(ab.Reason) ? "Remote abort" : ab.Reason);
                return;
            }

            if (msg is SystemMsg sys)
            {
                // ✅ 心跳：避免刷屏，不写 log
                if (sys.Text != null && sys.Text.StartsWith(PingText, StringComparison.Ordinal))
                {
                    // 收到 ping -> 回 pong（异步即可）
                    _ = SendAsync(new SystemMsg { Text = PongText });
                    return;
                }
                if (sys.Text != null && sys.Text.StartsWith(PongText, StringComparison.Ordinal))
                    return;

                Log("[SYS] " + sys.Text);
                return;
            }
        }

        private void Log(string s) => OnLog?.Invoke(s);

        // =========================
        // Shutdown / Dispose
        // =========================
        public async Task ShutdownAsync(string reason = "Shutdown")
        {
            if (Interlocked.Exchange(ref _shuttingDown, 1) == 1) return;

            // ✅ 防止 shutdown 过程中心跳仍在发包
            StopHeartbeat();

            try
            {
                if (_host != null)
                {
                    try { await _host.StopAsync().ConfigureAwait(false); } catch { }
                    try { _host.Dispose(); } catch { }
                    _host = null;
                }

                if (_joiner != null)
                {
                    try { await _joiner.DisconnectAsync(reason).ConfigureAwait(false); } catch { }
                    try { _joiner.Dispose(); } catch { }
                    _joiner = null;
                }

                IsConnected = false;
                IsHost = false;
                LocalSlot = 0;

                // ✅ 注意：不清掉 LastSummarySlot1/2（GameOver 需要）
                LastStartMsg = null;
                LastSelectSongMsg = null;

                Slot1Difficulty = null;
                Slot2Difficulty = null;
                Slot1Ready = false;
                Slot2Ready = false;
            }
            finally
            {
                Interlocked.Exchange(ref _shuttingDown, 0);
            }
        }

        public void Dispose()
        {
            try { ShutdownAsync("Dispose").GetAwaiter().GetResult(); } catch { }
        }

        // =========================
        // IP picking helpers
        // =========================
        private static string ChooseBestShareIp(string[] ips)
        {
            if (ips == null || ips.Length == 0) return null;

            // 优先 RFC1918 私网段（最符合“发给朋友同一局域网连接”）
            var privateIp = ips.FirstOrDefault(IsRfc1918);
            if (!string.IsNullOrWhiteSpace(privateIp)) return privateIp;

            // 其次就用第一个可用 IP（已经过滤 169.254）
            return ips[0];
        }

        private static bool IsRfc1918(string ip)
        {
            if (!IPAddress.TryParse(ip, out var addr)) return false;
            if (addr.AddressFamily != AddressFamily.InterNetwork) return false;

            var b = addr.GetAddressBytes();
            if (b[0] == 10) return true;
            if (b[0] == 192 && b[1] == 168) return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
            return false;
        }

        private static string[] GetLanIPv4Addresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ua => ua.Address.ToString())
                    .Where(ip => !ip.StartsWith("169.254."))
                    .Distinct()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
